using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Sophia.Web.Apoio;
using Sophia.Web.Controllers;

namespace Sophia.Integration.Tests.Web;

public sealed class PaginaDeErroTests : IAsyncLifetime
{
    private const string EnvioRecusado = "Não foi possível conferir o envio do formulário. Recarregue a página e tente de novo.";
    private const string EnderecoInvalido = "O endereço tem um valor que não foi entendido. Volte à lista e tente de novo.";

    private WebApplication _aplicacao = null!;

    public async Task InitializeAsync()
    {
        var construtor = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(ErroController).Assembly.GetName().Name,
            EnvironmentName = "Production",
        });
        construtor.Configuration["ConnectionStrings:Sophia"] = "Server=.;Database=SemUso;Integrated Security=true;TrustServerCertificate=true";
        construtor.WebHost.UseUrls("http://127.0.0.1:0");
        construtor.AdicionarSophia();
        _aplicacao = construtor.Build();
        _aplicacao.UsarSophia();
        await _aplicacao.StartAsync();
    }

    public async Task DisposeAsync() => await _aplicacao.DisposeAsync();

    [Fact]
    public async Task Envio_com_token_invalido_mostra_a_pagina_de_envio_recusado_com_corpo()
    {
        using var corpo = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nome"] = "Caneta azul",
            ["ValorUnitario"] = "2,50",
            ["__RequestVerificationToken"] = "token-de-outra-sessao",
        });

        using var http = Cliente();
        using var resposta = await http.PostAsync(new Uri("/Produtos/Novo", UriKind.Relative), corpo);
        var pagina = await resposta.Content.ReadAsStringAsync();

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        pagina.Should().Contain("<h1>Algo deu errado</h1>").And.Contain(EnvioRecusado).And.NotContain("endereço");
    }

    [Fact]
    public async Task Consulta_com_valor_invalido_mostra_a_pagina_de_endereco_invalido()
    {
        using var http = Cliente();
        using var resposta = await http.GetAsync(new Uri("/Vendas?pagina=abc", UriKind.Relative));
        var pagina = await resposta.Content.ReadAsStringAsync();

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        pagina.Should().Contain(EnderecoInvalido).And.NotContain(EnvioRecusado);
    }

    [Fact]
    public async Task Consulta_head_com_valor_invalido_continua_head_e_volta_sem_corpo()
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Head, new Uri("/Erro?codigo=abc", UriKind.Relative));

        using var http = Cliente();
        using var resposta = await http.SendAsync(pedido);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        (await resposta.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
    }

    private HttpClient Cliente() => new() { BaseAddress = new Uri(_aplicacao.Urls.Single()) };
}
