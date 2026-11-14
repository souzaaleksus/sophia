using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Sophia.Web.Apoio;

namespace Sophia.Integration.Tests.Web;

public sealed class ConsultaInvalidaTests
{
    [Fact]
    public void Fica_registrada_entre_os_filtros_do_mvc()
    {
        var opcoes = new MvcOptions().RecusarConsultasInvalidas();

        opcoes.Filters.OfType<RecusarConsultaInvalida>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task Consulta_com_valor_invalido_e_recusada_sem_chegar_a_acao(string metodo)
    {
        var (contexto, chegou) = await ExecutarAsync(metodo, valido: false);

        contexto.Result.Should().BeOfType<BadRequestResult>();
        chegou.Should().BeFalse();
    }

    [Fact]
    public async Task Consulta_valida_chega_a_acao()
    {
        var (contexto, chegou) = await ExecutarAsync(HttpMethods.Get, valido: true);

        contexto.Result.Should().BeNull();
        chegou.Should().BeTrue();
    }

    [Fact]
    public async Task Envio_de_formulario_invalido_chega_a_acao_para_mostrar_os_erros()
    {
        var (contexto, chegou) = await ExecutarAsync(HttpMethods.Post, valido: false);

        contexto.Result.Should().BeNull();
        chegou.Should().BeTrue();
    }

    private static async Task<(ActionExecutingContext Contexto, bool Chegou)> ExecutarAsync(string metodo, bool valido)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = metodo;
        var acao = new ActionContext(http, new RouteData(), new ActionDescriptor());
        if (!valido)
        {
            acao.ModelState.AddModelError("id", "O valor 'abc' não é válido.");
        }

        var contexto = new ActionExecutingContext(acao, [], new Dictionary<string, object?>(), new object());
        var chegou = false;
        await new RecusarConsultaInvalida().OnActionExecutionAsync(contexto, () =>
        {
            chegou = true;
            return Task.FromResult(new ActionExecutedContext(acao, [], new object()));
        });

        return (contexto, chegou);
    }
}
