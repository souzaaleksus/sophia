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
    public async Task<IActionResult> Index([FromQuery] ConsultaDeLista consulta, CancellationToken cancelamento) =>
        this.ExibirLista(consulta, await produtos.ListarAsync(consulta, cancelamento));

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
    public IActionResult Novo() => View(AcoesDeCadastro.Formulario, new ProdutoFormulario());

    [HttpPost]
    public Task<IActionResult> Novo(ProdutoFormulario formulario, CancellationToken cancelamento) =>
        this.GravarAsync(formulario, () => produtos.CadastrarAsync(formulario.ParaDados(), cancelamento), textos["produtos.cadastrado"]);

    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancelamento)
    {
        var produto = await produtos.ObterAsync(id, cancelamento);
        return produto is null ? NotFound() : View(AcoesDeCadastro.Formulario, ProdutoFormulario.De(produto));
    }

    [HttpPost]
    public Task<IActionResult> Editar(int id, ProdutoFormulario formulario, CancellationToken cancelamento)
    {
        formulario.Id = id;
        return this.GravarAsync(formulario, () => produtos.AlterarAsync(id, formulario.ParaDados(), cancelamento), textos["produtos.alterado"]);
    }

    [HttpGet]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancelamento)
    {
        var produto = await produtos.ObterAsync(id, cancelamento);
        return produto is null ? NotFound() : View(produto);
    }

    [HttpPost]
    [ActionName(nameof(Excluir))]
    public Task<IActionResult> ConfirmarExclusao(int id, CancellationToken cancelamento) =>
        this.ExcluirAsync(() => produtos.ExcluirAsync(id, cancelamento), textos["produtos.excluido"]);
}
