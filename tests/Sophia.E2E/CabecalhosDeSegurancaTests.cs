using Sophia.E2E.Apoio;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class CabecalhosDeSegurancaTests(AplicacaoEmExecucao aplicacao)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Vendas/Nova")]
    [InlineData("/css/site.css")]
    [InlineData("/endereco-que-nao-existe")]
    public async Task Toda_resposta_traz_os_cabecalhos_de_protecao(string caminho)
    {
        using var http = new HttpClient();

        using var resposta = await http.GetAsync(new Uri($"{aplicacao.Endereco}{caminho}"));

        Cabecalho(resposta, "X-Content-Type-Options").Should().Be("nosniff");
        Cabecalho(resposta, "X-Frame-Options").Should().Be("DENY");
        Cabecalho(resposta, "Content-Security-Policy").Should().Be(
            "default-src 'self'; script-src 'self'; style-src 'self' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; " +
            "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'");
        Cabecalho(resposta, "Referrer-Policy").Should().Be("strict-origin-when-cross-origin");
    }

    private static string Cabecalho(HttpResponseMessage resposta, string nome) =>
        resposta.Headers.TryGetValues(nome, out var valores) || resposta.Content.Headers.TryGetValues(nome, out valores)
            ? string.Join(",", valores)
            : string.Empty;
}
