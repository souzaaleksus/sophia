using Reqnroll;
using Sophia.Application;
using Sophia.Application.Vendas;
using Sophia.Domain.Vendas;
using Sophia.Specs.Apoio;

namespace Sophia.Specs.Passos;

[Binding]
public sealed class PassosDaConsultaDeVendas(Cenario cenario)
{
    private Pagina<VendaResumoDto>? _pagina;

    [Given(@"^que foram registradas (\d+) vendas, uma por dia, até (\d{2}/\d{2}/\d{4})$")]
    public async Task DadoQueForamRegistradasVendasUmaPorDia(int quantidade, string ate)
    {
        var cliente = await cenario.ClienteChamadoAsync("Maria Santos");
        var produto = await cenario.ProdutoChamadoAsync("Caneta azul");
        var ultimoDia = Cenario.Data(ate)!.Value;

        for (var dias = quantidade - 1; dias >= 0; dias--)
        {
            var dados = new DadosVenda(
                ultimoDia.AddDays(-dias),
                cliente.Id,
                [new(produto.Id, 1, produto.ValorUnitario)],
                Guid.NewGuid());
            (await cenario.Vendas.RegistrarAsync(dados)).Sucesso.Should().BeTrue();
        }
    }

    [When(@"^eu consultar a página (-?\d+) das vendas$")]
    public async Task QuandoEuConsultarAPagina(int numero)
    {
        _pagina = await cenario.Vendas.ListarAsync(numero);
    }

    [Then(@"^vejo (\d+) vendas, de (\d{2}/\d{2}/\d{4}) a (\d{2}/\d{2}/\d{4})$")]
    public void EntaoVejoAsVendas(int quantidade, string maisRecente, string maisAntiga)
    {
        _pagina!.Itens.Should().HaveCount(quantidade);
        _pagina.Itens.Select(v => v.Data).Should().BeInDescendingOrder();
        _pagina.Itens[0].Data.Should().Be(Cenario.Data(maisRecente));
        _pagina.Itens[^1].Data.Should().Be(Cenario.Data(maisAntiga));
    }

    [Then(@"^estou na página (\d+) de (\d+), com as vendas (\d+) a (\d+) de (\d+)$")]
    public void EntaoEstouNaPagina(int numero, int totalDePaginas, int primeira, int ultima, int total)
    {
        _pagina!.Numero.Should().Be(numero);
        _pagina.TotalDePaginas.Should().Be(totalDePaginas);
        _pagina.PrimeiroItem.Should().Be(primeira);
        _pagina.UltimoItem.Should().Be(ultima);
        _pagina.TotalDeItens.Should().Be(total);
    }

    [Then(@"^há página (anterior|seguinte)$")]
    public void EntaoHaPagina(string qual)
    {
        Existe(qual).Should().BeTrue();
    }

    [Then(@"^não há página (anterior|seguinte)$")]
    public void EntaoNaoHaPagina(string qual)
    {
        Existe(qual).Should().BeFalse();
    }

    private bool Existe(string qual) => qual == "anterior" ? _pagina!.TemAnterior : _pagina!.TemProxima;
}
