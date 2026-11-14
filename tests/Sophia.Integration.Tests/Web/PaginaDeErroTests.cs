using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sophia.Web.Apoio;

namespace Sophia.Integration.Tests.Web;

public sealed class PaginaDeErroTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task A_pagina_de_erro_e_lida_como_consulta_e_guarda_o_metodo_do_pedido_que_falhou(string metodo)
    {
        var contexto = Reexecucao(metodo);
        var (metodoNaPagina, metodoOriginal) = (string.Empty, string.Empty);

        await Montar(http =>
        {
            metodoNaPagina = http.Request.Method;
            metodoOriginal = PaginaDeErro.MetodoOriginal(http);
        })(contexto);

        metodoNaPagina.Should().Be(HttpMethods.Get);
        metodoOriginal.Should().Be(metodo);
        contexto.Request.Method.Should().Be(metodo);
    }

    [Fact]
    public async Task Fora_da_pagina_de_erro_o_metodo_original_e_o_do_proprio_pedido()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = HttpMethods.Post;
        var metodoOriginal = string.Empty;

        await Montar(http => metodoOriginal = PaginaDeErro.MetodoOriginal(http))(contexto);

        metodoOriginal.Should().Be(HttpMethods.Post);
    }

    private static DefaultHttpContext Reexecucao(string metodo)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = metodo;
        contexto.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature { OriginalPath = "/Produtos/Novo" });
        return contexto;
    }

    private static RequestDelegate Montar(Action<HttpContext> pagina)
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        app.ExibirErroComoLeitura();
        app.Run(http =>
        {
            pagina(http);
            return Task.CompletedTask;
        });
        return app.Build();
    }
}
