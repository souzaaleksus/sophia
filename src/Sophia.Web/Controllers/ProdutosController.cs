using Sophia.Application;
using Sophia.Application.Produtos;
using Sophia.Domain.Produtos;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class ProdutosController(
    IServicoDeCadastro<ProdutoDto, DadosProduto> produtos,
    ParametrosDoSophia parametros)
    : CadastroController<ProdutoDto, ProdutoFormulario, DadosProduto>(
        produtos,
        new("Produto cadastrado.", "Produto alterado.", "Produto excluído."),
        parametros)
{
    protected override ProdutoFormulario ParaFormulario(ProdutoDto registro) => ProdutoFormulario.De(registro);

    protected override object Sugestao(ProdutoDto registro) => new { registro.Id, registro.Nome, registro.ValorUnitario };
}
