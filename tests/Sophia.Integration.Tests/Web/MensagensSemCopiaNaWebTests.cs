using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Sophia.Integration.Tests.Web;

public sealed partial class MensagensSemCopiaNaWebTests
{
    private static readonly DirectoryInfo Fontes = LocalizarFontes();

    [Theory]
    [InlineData("Sophia.Domain")]
    [InlineData("Sophia.Application")]
    public void Nenhum_texto_da_web_repete_uma_mensagem_de_outra_camada(string projeto)
    {
        var mensagens = MensagensDe(projeto);

        var copias = ArquivosDaWeb()
            .SelectMany(arquivo =>
            {
                var conteudo = File.ReadAllText(arquivo.FullName);
                return mensagens.Where(mensagem => Repete(conteudo, mensagem)).Select(mensagem => $"{arquivo.Name}: {mensagem}");
            });

        copias.Should().BeEmpty("a Web usa a mensagem da camada que a define, sem copiar o texto");
    }

    [Theory]
    [InlineData("data-val-maximo=\"O valor unitário está acima do permitido.\"", "O valor unitário está acima do permitido.")]
    [InlineData("data-val-maximo=\"Valor unitário está acima do permitido.\"", "O valor unitário está acima do permitido.")]
    [InlineData("return \"a QUANTIDADE deve ser maior que zero.\";", "A quantidade deve ser maior que zero.")]
    [InlineData("$\"Quantidade aceita no máximo {limite} por item.\"", "A quantidade aceita no máximo {0} por item.")]
    public void A_copia_e_achada_inteira_ou_sem_o_artigo_sem_diferenciar_maiusculas(string trecho, string mensagem)
    {
        Repete(trecho, mensagem).Should().BeTrue();
    }

    [Theory]
    [InlineData("$\"Informe {campo}.\"", "Informe o CPF.")]
    [InlineData("<span>Valor unitário</span>", "O valor unitário está acima do permitido.")]
    [InlineData("<p>Quantidade</p> aceita no máximo", "A quantidade aceita no máximo {0} por item.")]
    public void Texto_que_nao_repete_a_mensagem_passa(string trecho, string mensagem)
    {
        Repete(trecho, mensagem).Should().BeFalse();
    }

    private static bool Repete(string conteudo, string mensagem)
    {
        var semArtigo = Artigo().Replace(mensagem, string.Empty);
        var padrao = string.Join("[^\r\n\"]+?", Marcador().Split(semArtigo).Select(Regex.Escape));
        return Regex.IsMatch(conteudo, padrao, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    }

    private static List<string> MensagensDe(string projeto) =>
        XDocument.Load(Path.Combine(Fontes.FullName, projeto, "Recursos", "Mensagens.resx")).Root!
            .Elements("data")
            .Select(dado => dado.Element("value")!.Value)
            .ToList();

    private static IEnumerable<FileInfo> ArquivosDaWeb() =>
        new DirectoryInfo(Path.Combine(Fontes.FullName, "Sophia.Web"))
            .EnumerateFiles("*.*", SearchOption.AllDirectories)
            .Where(arquivo => arquivo.Extension is ".cshtml" or ".cs")
            .Where(arquivo => !arquivo.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(arquivo => !arquivo.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static DirectoryInfo LocalizarFontes()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Sophia.sln")))
        {
            pasta = pasta.Parent;
        }

        return new DirectoryInfo(Path.Combine(pasta!.FullName, "src"));
    }

    [GeneratedRegex(@"^(?:O|A) ")]
    private static partial Regex Artigo();

    [GeneratedRegex(@"\{[0-9]+\}")]
    private static partial Regex Marcador();
}
