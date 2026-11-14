using Microsoft.AspNetCore.Mvc;
using Sophia.Application.Vendas;
using Sophia.Web.Apoio;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class VendasController(IVendaServico vendas) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int pagina = 1,
        DateOnly? de = null,
        DateOnly? ate = null,
        string? ordem = null,
        CancellationToken cancelamento = default)
    {
        var consulta = new ConsultaDeVendas(pagina, de, ate, ordem);
        var lista = await vendas.ListarAsync(consulta, cancelamento);
        ViewData["De"] = consulta.Inicio;
        ViewData["Ate"] = consulta.Fim;
        ViewData["Ordem"] = lista.Ordem;
        return View(lista.Pagina);
    }

    [HttpGet]
    public IActionResult Nova() => ExibirNovaVenda(new NovaVendaFormulario
    {
        Chave = Guid.NewGuid(),
        Data = vendas.DataSugerida(),
    });

    [HttpPost]
    public async Task<IActionResult> Nova(NovaVendaFormulario formulario, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return ExibirNovaVenda(formulario);
        }

        var resultado = await vendas.RegistrarAsync(formulario.ParaDados(), cancelamento);
        if (!resultado.Sucesso)
        {
            ModelState.AdicionarErros(resultado.Erros);
            return ExibirNovaVenda(formulario);
        }

        TempData.Sucesso("Venda registrada.");
        return RedirectToAction(nameof(Detalhe), new { id = resultado.Valor });
    }

    [HttpGet]
    public async Task<IActionResult> Detalhe(int id, CancellationToken cancelamento)
    {
        var venda = await vendas.ObterAsync(id, cancelamento);
        return venda is null ? NotFound() : View(venda);
    }

    private ViewResult ExibirNovaVenda(NovaVendaFormulario formulario)
    {
        if (formulario.Itens.Count == 0)
        {
            formulario.Itens.Add(new ItemDaVendaFormulario());
        }

        ViewData["Hoje"] = vendas.DataSugerida();
        return View("Nova", formulario);
    }
}
