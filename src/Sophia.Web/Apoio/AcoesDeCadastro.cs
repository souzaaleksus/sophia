using Microsoft.AspNetCore.Mvc;
using Sophia.Application;
using Sophia.Domain;

namespace Sophia.Web.Apoio;

public static class AcoesDeCadastro
{
    public const string Formulario = "Formulario";

    public static ViewResult ExibirLista<T>(this Controller controller, ConsultaDeLista consulta, Lista<T> lista)
    {
        controller.ViewData["Busca"] = consulta.Busca;
        controller.ViewData["Ordem"] = lista.Ordem;
        return controller.View(lista.Pagina);
    }

    public static async Task<IActionResult> GravarAsync<TResultado>(
        this Controller controller,
        object formulario,
        Func<Task<TResultado>> gravar,
        string mensagemDeSucesso)
        where TResultado : Resultado
    {
        if (!controller.ModelState.IsValid)
        {
            return controller.View(Formulario, formulario);
        }

        var resultado = await gravar();
        if (!resultado.Sucesso)
        {
            controller.ModelState.AdicionarErros(resultado.Erros);
            return controller.View(Formulario, formulario);
        }

        controller.TempData.Sucesso(mensagemDeSucesso);
        return controller.RedirectToAction("Index");
    }

    public static async Task<IActionResult> ExcluirAsync(
        this Controller controller,
        Func<Task<Resultado>> excluir,
        string mensagemDeSucesso)
    {
        if (!controller.ModelState.IsValid)
        {
            return controller.BadRequest();
        }

        var resultado = await excluir();
        if (resultado.Sucesso)
        {
            controller.TempData.Sucesso(mensagemDeSucesso);
        }
        else
        {
            controller.TempData.Falha(resultado.Erros[0].Mensagem);
        }

        return controller.RedirectToAction("Index");
    }
}
