using Microsoft.AspNetCore.Mvc;
using Sophia.Application.Clientes;
using Sophia.Web.Apoio;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class ClientesController(IClienteServico clientes) : Controller
{
    private const int SugestoesNaBusca = 8;

    [HttpGet]
    public async Task<IActionResult> Index(string? busca, CancellationToken cancelamento)
    {
        ViewData["Busca"] = busca;
        return View(await clientes.ListarAsync(busca, cancelamento: cancelamento));
    }

    [HttpGet]
    public async Task<IActionResult> Buscar(string? termo, CancellationToken cancelamento)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return Json(Array.Empty<object>());
        }

        var encontrados = await clientes.ListarAsync(termo, SugestoesNaBusca, cancelamento);
        return Json(encontrados.Select(c => new { c.Id, c.Cpf, c.Nome }));
    }

    [HttpGet]
    public IActionResult Novo() => View("Formulario", new ClienteFormulario());

    [HttpPost]
    public async Task<IActionResult> Novo(ClienteFormulario formulario, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await clientes.CadastrarAsync(formulario.ParaDados(), cancelamento);
        if (!resultado.Sucesso)
        {
            ModelState.AdicionarErros(resultado.Erros);
            return View("Formulario", formulario);
        }

        TempData.Sucesso("Cliente cadastrado.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancelamento)
    {
        var cliente = await clientes.ObterAsync(id, cancelamento);
        return cliente is null ? NotFound() : View("Formulario", ClienteFormulario.De(cliente));
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, ClienteFormulario formulario, CancellationToken cancelamento)
    {
        formulario.Id = id;
        if (!ModelState.IsValid)
        {
            return View("Formulario", formulario);
        }

        var resultado = await clientes.AlterarAsync(id, formulario.ParaDados(), cancelamento);
        if (!resultado.Sucesso)
        {
            ModelState.AdicionarErros(resultado.Erros);
            return View("Formulario", formulario);
        }

        TempData.Sucesso("Cliente alterado.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancelamento)
    {
        var cliente = await clientes.ObterAsync(id, cancelamento);
        return cliente is null ? NotFound() : View(cliente);
    }

    [HttpPost]
    [ActionName(nameof(Excluir))]
    public async Task<IActionResult> ConfirmarExclusao(int id, CancellationToken cancelamento)
    {
        var resultado = await clientes.ExcluirAsync(id, cancelamento);
        if (resultado.Sucesso)
        {
            TempData.Sucesso("Cliente excluído.");
        }
        else
        {
            TempData.Falha(resultado.Erros[0].Mensagem);
        }

        return RedirectToAction(nameof(Index));
    }
}
