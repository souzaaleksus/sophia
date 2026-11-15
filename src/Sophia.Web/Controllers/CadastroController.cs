using Microsoft.AspNetCore.Mvc;
using Sophia.Application;
using Sophia.Domain;
using Sophia.Web.Apoio;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public abstract class CadastroController<TDto, TFormulario, TDados>(
    IServicoDeCadastro<TDto, TDados> servico,
    AvisosDeCadastro avisos,
    ParametrosDoSophia parametros)
    : Controller
    where TDto : class
    where TFormulario : class, IFormularioDeCadastro<TDados>, new()
{
    private const string Formulario = "Formulario";

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ConsultaDeLista consulta, CancellationToken cancelamento)
    {
        var lista = await servico.ListarAsync(consulta, cancelamento);
        ViewData["Busca"] = consulta.Busca;
        ViewData["Ordem"] = lista.Ordem;
        return View(lista.Pagina);
    }

    [HttpGet]
    public async Task<IActionResult> Buscar(string? termo, CancellationToken cancelamento)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return Json(Array.Empty<object>());
        }

        var encontrados = await servico.BuscarAsync(termo, parametros.SugestoesNaBusca, cancelamento);
        return Json(encontrados.Select(Sugestao));
    }

    [HttpGet]
    public IActionResult Novo() => View(Formulario, new TFormulario());

    [HttpPost]
    public Task<IActionResult> Novo(TFormulario formulario, CancellationToken cancelamento) =>
        GravarAsync(formulario, () => servico.CadastrarAsync(formulario.ParaDados(), cancelamento), avisos.Cadastrado);

    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancelamento)
    {
        var registro = await servico.ObterAsync(id, cancelamento);
        return registro is null ? NotFound() : View(Formulario, ParaFormulario(registro));
    }

    [HttpPost]
    public Task<IActionResult> Editar(int id, TFormulario formulario, CancellationToken cancelamento)
    {
        formulario.Id = id;
        return GravarAsync(formulario, () => servico.AlterarAsync(id, formulario.ParaDados(), cancelamento), avisos.Alterado);
    }

    [HttpGet]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancelamento)
    {
        var registro = await servico.ObterAsync(id, cancelamento);
        return registro is null ? NotFound() : View(registro);
    }

    [HttpPost]
    [ActionName(nameof(Excluir))]
    public async Task<IActionResult> ConfirmarExclusao(int id, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var resultado = await servico.ExcluirAsync(id, cancelamento);
        if (resultado.Sucesso)
        {
            TempData.Sucesso(avisos.Excluido);
        }
        else
        {
            TempData.Falha(resultado.Erros[0].Mensagem);
        }

        return RedirectToAction(nameof(Index));
    }

    protected abstract TFormulario ParaFormulario(TDto registro);

    protected abstract object Sugestao(TDto registro);

    private async Task<IActionResult> GravarAsync<TResultado>(TFormulario formulario, Func<Task<TResultado>> gravar, string aviso)
        where TResultado : Resultado
    {
        if (!ModelState.IsValid)
        {
            return View(Formulario, formulario);
        }

        var resultado = await gravar();
        if (!resultado.Sucesso)
        {
            ModelState.AdicionarErros(resultado.Erros);
            return View(Formulario, formulario);
        }

        TempData.Sucesso(aviso);
        return RedirectToAction(nameof(Index));
    }
}
