using Sophia.Web.Models;

namespace Sophia.Integration.Tests.Web;

public sealed class ColunaOrdenavelTests
{
    [Theory]
    [InlineData("nome", "#icone-ordem")]
    [InlineData("data", "#icone-ordem-crescente")]
    [InlineData("-data", "#icone-ordem-decrescente")]
    public void Icone_mostra_se_a_coluna_ordena_a_lista_e_em_que_sentido(string ordemAtual, string icone)
    {
        var coluna = new ColunaOrdenavel("Data", "data", ordemAtual, new Dictionary<string, string>());

        coluna.Icone.Should().Be(icone);
    }
}
