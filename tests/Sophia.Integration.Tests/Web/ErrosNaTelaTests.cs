using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sophia.Domain;
using Sophia.Web.Apoio;

namespace Sophia.Integration.Tests.Web;

public sealed class ErrosNaTelaTests
{
    [Fact]
    public void Erro_de_um_item_vai_para_o_campo_da_linha_na_tela()
    {
        var modelState = new ModelStateDictionary();

        modelState.AdicionarErros([new Erro("ProdutoId", "Selecione o produto.", 2)]);

        modelState.PrimeiroErro("Itens[2].ProdutoId").Should().Be("Selecione o produto.");
    }

    [Theory]
    [InlineData("Itens")]
    [InlineData("Chave")]
    [InlineData("ClienteId")]
    [InlineData("")]
    public void Erro_sem_linha_vai_para_o_proprio_campo(string campo)
    {
        var modelState = new ModelStateDictionary();

        modelState.AdicionarErros([new Erro(campo, "Mensagem.")]);

        modelState.Keys.Should().Equal(campo);
        modelState.PrimeiroErro(campo).Should().Be("Mensagem.");
    }
}
