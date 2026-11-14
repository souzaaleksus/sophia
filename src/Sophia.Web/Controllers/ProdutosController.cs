using Microsoft.Extensions.Localization;
using Sophia.Application;
using Sophia.Application.Produtos;
using Sophia.Domain.Produtos;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class ProdutosController(IProdutoServico produtos, ParametrosDoSophia parametros, IStringLocalizer<Textos> textos)
    : CadastroController<ProdutoDto, ProdutoFormulario, DadosProduto>(
        new(produtos.ListarAsync, produtos.BuscarAsync, produtos.ObterAsync, produtos.CadastrarAsync, produtos.AlterarAsync, produtos.ExcluirAsync),
        new(textos["produtos.cadastrado"], textos["produtos.alterado"], textos["produtos.excluido"]),
        parametros)
{
    protected override ProdutoFormulario ParaFormulario(ProdutoDto registro) => ProdutoFormulario.De(registro);

    protected override object Sugestao(ProdutoDto registro) => new { registro.Id, registro.Nome, registro.ValorUnitario };
}
