using System.Text.RegularExpressions;
using Sophia.E2E.Apoio;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed partial class CarregamentoDosScriptsTests(AplicacaoEmExecucao aplicacao)
{
    [Theory]
    [InlineData("/js/site.js")]
    [InlineData("/js/comum/mascaras.js")]
    [InlineData("/js/caixa/linhas.js")]
    public async Task Todo_modulo_de_javascript_e_revalidado_a_cada_carga(string caminho)
    {
        using var http = new HttpClient();

        using var resposta = await http.GetAsync(new Uri($"{aplicacao.Endereco}{caminho}"));

        resposta.IsSuccessStatusCode.Should().BeTrue();
        resposta.Headers.CacheControl?.NoCache.Should().BeTrue($"{caminho} não pode ficar velho no navegador depois de um deploy");
    }

    [Fact]
    public async Task Todo_modulo_importado_pelo_site_js_e_baixado_junto_com_a_pagina()
    {
        using var http = new HttpClient();
        var composicao = await http.GetStringAsync(new Uri($"{aplicacao.Endereco}/js/site.js"));
        var pagina = await http.GetStringAsync(new Uri($"{aplicacao.Endereco}/"));

        var importados = Importacao().Matches(composicao).Select(m => m.Groups["modulo"].Value).ToList();

        importados.Should().NotBeEmpty();
        foreach (var modulo in importados)
        {
            pagina.Should().Contain($"<link rel=\"modulepreload\" href=\"/js/comum/{modulo}\"", $"{modulo} precisa começar a baixar junto com a página");
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Vendas")]
    [InlineData("/Vendas/Nova")]
    [InlineData("/Clientes")]
    [InlineData("/Produtos")]
    public async Task Tela_sem_formulario_de_cadastro_nao_baixa_o_jquery(string caminho)
    {
        using var http = new HttpClient();

        var pagina = await http.GetStringAsync(new Uri($"{aplicacao.Endereco}{caminho}"));

        pagina.Should().NotContain("/lib/jquery", $"{caminho} não usa a validação do jQuery");
    }

    [Theory]
    [InlineData("/Clientes/Novo")]
    [InlineData("/Produtos/Novo")]
    public async Task Formulario_de_cadastro_baixa_o_jquery_antes_da_validacao(string caminho)
    {
        using var http = new HttpClient();

        var pagina = await http.GetStringAsync(new Uri($"{aplicacao.Endereco}{caminho}"));

        var jquery = pagina.IndexOf("/lib/jquery/dist/jquery.min.js", StringComparison.Ordinal);
        jquery.Should().BeGreaterThan(-1);
        jquery.Should().BeLessThan(pagina.IndexOf("/lib/jquery-validation/dist/jquery.validate.min.js", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("/lib/jquery/dist/jquery.js")]
    [InlineData("/lib/jquery-validation/dist/additional-methods.js")]
    [InlineData("/lib/jquery-validation/dist/additional-methods.min.js")]
    [InlineData("/lib/jquery-validation/dist/jquery.validate.js")]
    [InlineData("/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js")]
    public async Task Biblioteca_sem_uso_nao_e_publicada(string caminho)
    {
        using var http = new HttpClient();

        using var resposta = await http.GetAsync(new Uri($"{aplicacao.Endereco}{caminho}"));

        resposta.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [GeneratedRegex(@"import '\./comum/(?<modulo>[\w-]+\.js)';")]
    private static partial Regex Importacao();
}
