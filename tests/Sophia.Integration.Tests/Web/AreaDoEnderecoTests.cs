using Sophia.Web.Controllers;

namespace Sophia.Integration.Tests.Web;

public sealed class AreaDoEnderecoTests
{
    [Theory]
    [InlineData("/Clientes/Editar/999", "Clientes")]
    [InlineData("/clientes", "Clientes")]
    [InlineData("/Produtos/Excluir/abc", "Produtos")]
    [InlineData("/Vendas", "Vendas")]
    [InlineData("/Vendas/Detalhe/7", "Vendas")]
    [InlineData("/", "Painel")]
    [InlineData("/NaoExiste/1", "Painel")]
    [InlineData("", "Painel")]
    [InlineData(null, "Painel")]
    public void A_area_vem_do_primeiro_trecho_do_endereco(string? caminho, string area)
    {
        ErroController.AreaDe(caminho).Should().Be(area);
    }
}
