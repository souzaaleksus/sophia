using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sophia.Application;
using Sophia.Application.Produtos;
using Sophia.Web.Apoio;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class ProdutosController(IProdutoServico produtos, ParametrosDoSophia parametros, IStringLocalizer<Textos> textos) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ConsultaDeLista consulta, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var lista = await produtos.ListarAsync(consulta, cancelamento);
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

        var encontrados = await produtos.BuscarAsync(termo, parametros.SugestoesNaBusca, cancelamento);
        return Json(encontrados.Select(p => new { p.Id, p.Nome, p.ValorUnitario }));
    }

    [HttpGet]
    public IActionResult Novo() => View("Formulario", new ProdutoFormulario());

    [HttpPost]
    public async Task<IActionResult> Novo(ProdutoFormulario formulario, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await produtos.CadastrarAsync(formulario.ParaDados(), cancelamento);
        if (!resultado.Sucesso)
        {
            ModelState.AdicionarErros(resultado.Erros);
            return View("Formulario", formulario);
        }

        TempData.Sucesso(textos["produtos.cadastrado"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var produto = await produtos.ObterAsync(id, cancelamento);
        return produto is null ? NotFound() : View("Formulario", ProdutoFormulario.De(produto));
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, ProdutoFormulario formulario, CancellationToken cancelamento)
    {
        formulario.Id = id;
        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await produtos.AlterarAsync(id, formulario.ParaDados(), cancelamento);
        if (!resultado.Sucesso)
        {
            ModelState.AdicionarErros(resultado.Erros);
            return View("Formulario", formulario);
        }

        TempData.Sucesso(textos["produtos.alterado"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var produto = await produtos.ObterAsync(id, cancelamento);
        return produto is null ? NotFound() : View(produto);
    }

    [HttpPost]
    [ActionName(nameof(Excluir))]
    public async Task<IActionResult> ConfirmarExclusao(int id, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var resultado = await produtos.ExcluirAsync(id, cancelamento);
        if (resultado.Sucesso)
        {
            TempData.Sucesso(textos["produtos.excluido"]);
        }
        else
        {
            TempData.Falha(resultado.Erros[0].Mensagem);
        }

        return RedirectToAction(nameof(Index));
    }
}
