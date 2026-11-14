using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sophia.Application;
using Sophia.Application.Clientes;
using Sophia.Web.Apoio;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class ClientesController(IClienteServico clientes, ParametrosDoSophia parametros, IStringLocalizer<Textos> textos) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ConsultaDeLista consulta, CancellationToken cancelamento) =>
        this.ExibirLista(consulta, await clientes.ListarAsync(consulta, cancelamento));

    [HttpGet]
    public async Task<IActionResult> Buscar(string? termo, CancellationToken cancelamento)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return Json(Array.Empty<object>());
        }

        var encontrados = await clientes.BuscarAsync(termo, parametros.SugestoesNaBusca, cancelamento);
        return Json(encontrados.Select(c => new { c.Id, c.Cpf, c.Nome }));
    }

    [HttpGet]
    public IActionResult Novo() => View(AcoesDeCadastro.Formulario, new ClienteFormulario());

    [HttpPost]
    public Task<IActionResult> Novo(ClienteFormulario formulario, CancellationToken cancelamento) =>
        this.GravarAsync(formulario, () => clientes.CadastrarAsync(formulario.ParaDados(), cancelamento), textos["clientes.cadastrado"]);

    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancelamento)
    {
        var cliente = await clientes.ObterAsync(id, cancelamento);
        return cliente is null ? NotFound() : View(AcoesDeCadastro.Formulario, ClienteFormulario.De(cliente));
    }

    [HttpPost]
    public Task<IActionResult> Editar(int id, ClienteFormulario formulario, CancellationToken cancelamento)
    {
        formulario.Id = id;
        return this.GravarAsync(formulario, () => clientes.AlterarAsync(id, formulario.ParaDados(), cancelamento), textos["clientes.alterado"]);
    }

    [HttpGet]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancelamento)
    {
        var cliente = await clientes.ObterAsync(id, cancelamento);
        return cliente is null ? NotFound() : View(cliente);
    }

    [HttpPost]
    [ActionName(nameof(Excluir))]
    public Task<IActionResult> ConfirmarExclusao(int id, CancellationToken cancelamento) =>
        this.ExcluirAsync(() => clientes.ExcluirAsync(id, cancelamento), textos["clientes.excluido"]);
}
