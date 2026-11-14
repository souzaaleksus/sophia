using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sophia.Application;
using Sophia.Application.Vendas;
using Sophia.Web.Apoio;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class VendasController(IVendaServico vendas, IStringLocalizer<Textos> textos) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ConsultaDeVendas consulta, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        return ExibirLista(consulta, await vendas.ListarAsync(consulta, cancelamento));
    }

    private ViewResult ExibirLista(ConsultaDeVendas consulta, Lista<VendaResumoDto> lista)
    {
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
            formulario.Chave = Guid.NewGuid();
            return ExibirNovaVenda(formulario);
        }

        TempData.Sucesso(textos["venda.registrada"]);
        return RedirectToAction(nameof(Detalhe), new { id = resultado.Valor });
    }

    [HttpGet]
    public async Task<IActionResult> Detalhe(int id, CancellationToken cancelamento)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

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
