using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Sophia.Integration.Tests.Web;

public sealed partial class TextosDaInterfaceTests
{
    private static readonly DirectoryInfo ProjetoWeb = LocalizarProjetoWeb();

    [Fact]
    public void Toda_chave_usada_na_interface_existe_no_recurso_de_textos()
    {
        var faltando = ChavesUsadas().Except(ChavesDoRecurso()).Order(StringComparer.Ordinal).ToList();

        faltando.Should().BeEmpty("cada texto da interface precisa estar em Recursos/Textos.resx");
    }

    [Fact]
    public void Nenhuma_chave_do_recurso_de_textos_fica_sem_uso()
    {
        var sobrando = ChavesDoRecurso().Except(ChavesUsadas()).Order(StringComparer.Ordinal).ToList();

        sobrando.Should().BeEmpty("texto que ninguém usa só confunde quem mantém o recurso");
    }

    [Fact]
    public void Nenhum_texto_do_recurso_fica_vazio()
    {
        var recurso = XDocument.Load(Path.Combine(ProjetoWeb.FullName, "Recursos", "Textos.resx"));

        var vazios = recurso.Root!.Elements("data")
            .Where(dado => string.IsNullOrWhiteSpace(dado.Element("value")?.Value))
            .Select(dado => dado.Attribute("name")!.Value);

        vazios.Should().BeEmpty();
    }

    private static HashSet<string> ChavesDoRecurso() =>
        XDocument.Load(Path.Combine(ProjetoWeb.FullName, "Recursos", "Textos.resx")).Root!
            .Elements("data")
            .Select(dado => dado.Attribute("name")!.Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ChavesUsadas() =>
        ProjetoWeb.EnumerateFiles("*.*", SearchOption.AllDirectories)
            .Where(arquivo => arquivo.Extension is ".cshtml" or ".cs")
            .Where(arquivo => !arquivo.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(arquivo => !arquivo.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(arquivo => UsoDeChave().Matches(File.ReadAllText(arquivo.FullName)))
            .Select(uso => uso.Groups["chave"].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static DirectoryInfo LocalizarProjetoWeb()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Sophia.sln")))
        {
            pasta = pasta.Parent;
        }

        return new DirectoryInfo(Path.Combine(pasta!.FullName, "src", "Sophia.Web"));
    }

    [GeneratedRegex(@"(?:\bT|\btextos)\[""(?<chave>[a-z0-9.-]+)""|(?:Name|ErrorMessage) = ""(?<chave>[a-z0-9-]+\.[a-z0-9.-]+)""")]
    private static partial Regex UsoDeChave();
}
