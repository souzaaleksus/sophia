using System.Text.RegularExpressions;

namespace Sophia.Integration.Tests.Web;

public sealed partial class FrasesSemRepeticaoNasViewsTests
{
    private const int TamanhoMinimo = 31;

    [Fact]
    public void Nenhuma_frase_visivel_longa_aparece_em_duas_views()
    {
        var repetidas = Views()
            .SelectMany(view => FrasesDe(File.ReadAllText(view.FullName)).Distinct().Select(frase => (frase, view: NomeDa(view))))
            .GroupBy(par => par.frase)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => $"\"{grupo.Key}\" em {string.Join(", ", grupo.Select(par => par.view))}");

        string.Join(Environment.NewLine, repetidas).Should().BeEmpty("a frase repetida em mais de uma view mora numa parcial");
    }

    [Theory]
    [InlineData("<p class=\"vazio-titulo\">Nenhuma venda registrada ainda.</p>", "Nenhuma venda registrada ainda.")]
    [InlineData("data-texto=\"A exclusão não pode ser desfeita. Clientes com vendas não podem ser excluídos.\">", "A exclusão não pode ser desfeita. Clientes com vendas não podem ser excluídos.")]
    [InlineData("A exclusão não pode ser desfeita. Clientes com vendas não podem ser excluídos.\n", "A exclusão não pode ser desfeita. Clientes com vendas não podem ser excluídos.")]
    [InlineData("        \"A exclusão não pode ser desfeita. Produtos já vendidos não podem ser excluídos.\",", "A exclusão não pode ser desfeita. Produtos já vendidos não podem ser excluídos.")]
    [InlineData("<a>\n    Escolha o cliente, lance os produtos e finalize.\n</a>", "Escolha o cliente, lance os produtos e finalize.")]
    public void A_frase_e_achada_no_texto_no_atributo_e_no_codigo(string trecho, string frase)
    {
        FrasesDe(trecho).Should().Contain(frase);
    }

    [Theory]
    [InlineData("<p>Curta demais para contar.</p>")]
    [InlineData("<div class=\"cartao formulario grade-de-campos rodape\">")]
    [InlineData("aria-label=\"Excluir @cliente.Nome do cadastro da loja\"")]
    public void Classe_texto_curto_e_texto_com_expressao_nao_contam(string trecho)
    {
        FrasesDe(trecho).Should().BeEmpty();
    }

    private static IEnumerable<string> FrasesDe(string conteudo) =>
        EntreMarcas().Matches(conteudo).SelectMany(achado => achado.Groups[1].Value.Split('\n'))
            .Concat(EntreAspas().Matches(conteudo).Select(achado => achado.Groups[1].Value))
            .Concat(conteudo.Split('\n').Where(linha => !linha.Contains('"', StringComparison.Ordinal)))
            .Select(trecho => Espacos().Replace(trecho, " ").Trim())
            .Where(trecho => trecho.Length >= TamanhoMinimo && Frase().IsMatch(trecho) && !trecho.Contains('@', StringComparison.Ordinal));

    private static IEnumerable<FileInfo> Views()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Sophia.sln")))
        {
            pasta = pasta.Parent;
        }

        return new DirectoryInfo(Path.Combine(pasta!.FullName, "src", "Sophia.Web", "Views")).EnumerateFiles("*.cshtml", SearchOption.AllDirectories);
    }

    private static string NomeDa(FileInfo view) => $"{view.Directory!.Name}/{view.Name}";

    [GeneratedRegex(@">([^<>]+)<")]
    private static partial Regex EntreMarcas();

    [GeneratedRegex("\"([^\"\r\n]+)\"")]
    private static partial Regex EntreAspas();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();

    [GeneratedRegex(@"^\p{Lu}[^{}=;<>]* [^{}=;<>]*$")]
    private static partial Regex Frase();
}
