using Reqnroll;
using Sophia.Application.Painel;
using Sophia.Domain.Vendas;
using Sophia.Specs.Apoio;

namespace Sophia.Specs.Passos;

[Binding]
public sealed class PassosDoPainel(Cenario cenario)
{
    private PainelDto? _painel;

    [Given("que foram registradas as vendas")]
    public async Task DadoQueForamRegistradasAsVendas(DataTable vendas)
    {
        foreach (var linha in vendas.Rows)
        {
            var cliente = await cenario.ClienteChamadoAsync(linha["Cliente"]);
            var produto = await cenario.ProdutoChamadoAsync(linha["Produto"]);
            var item = new DadosItemVenda(
                produto.Id,
                int.Parse(linha["Quantidade"], Cenario.PtBr),
                Cenario.Valor(linha["ValorUnitario"]));

            var resultado = await cenario.Vendas.RegistrarAsync(
                new DadosVenda(Cenario.Data(linha["Data"]), cliente.Id, [item]));

            resultado.Sucesso.Should().BeTrue();
        }
    }

    [When("eu abrir o painel")]
    public async Task QuandoEuAbrirOPainel()
    {
        _painel = await cenario.Painel.ObterAsync();
    }

    [Then(@"^vejo (\d+) vendas hoje somando ""(.*)""$")]
    public void EntaoVejoAsVendasDeHoje(int quantidade, string total)
    {
        _painel!.Dia.Should().Be(new ResumoDoPeriodo(quantidade, Cenario.Valor(total)!.Value));
    }

    [Then(@"^vejo (\d+) vendas no mês somando ""(.*)"" com ticket médio de ""(.*)""$")]
    public void EntaoVejoAsVendasDoMes(int quantidade, string total, string ticketMedio)
    {
        _painel!.Mes.Should().Be(new ResumoDoPeriodo(quantidade, Cenario.Valor(total)!.Value));
        _painel.Mes.TicketMedio.Should().Be(Cenario.Valor(ticketMedio));
    }

    [Then("não há últimas vendas")]
    public void EntaoNaoHaUltimasVendas()
    {
        _painel!.UltimasVendas.Should().BeEmpty();
    }

    [Then("os últimos {int} dias aparecem zerados")]
    public void EntaoOsUltimosDiasAparecemZerados(int dias)
    {
        _painel!.UltimosDias.Should().HaveCount(dias)
            .And.OnlyContain(d => d.Total == 0m && d.QuantidadeDeVendas == 0);
    }

    [Then("os últimos {int} dias mostram")]
    public void EntaoOsUltimosDiasMostram(int dias, DataTable esperados)
    {
        var totais = esperados.Rows.Select(linha => new TotalDoDia(
            Cenario.Data(linha["Dia"])!.Value,
            Cenario.Valor(linha["Total"])!.Value,
            int.Parse(linha["Vendas"], Cenario.PtBr)));

        _painel!.UltimosDias.Should().HaveCount(dias).And.Equal(totais);
    }

    [Then("não há produtos mais vendidos no mês")]
    public void EntaoNaoHaProdutosMaisVendidos()
    {
        _painel!.MaisVendidos.Should().BeEmpty();
    }

    [Then("os mais vendidos do mês são")]
    public void EntaoOsMaisVendidosDoMesSao(DataTable esperados)
    {
        var produtos = esperados.Rows.Select(linha => (
            linha["Produto"],
            int.Parse(linha["Quantidade"], Cenario.PtBr),
            Cenario.Valor(linha["Total"])!.Value));

        _painel!.MaisVendidos.Select(p => (p.Nome, p.Quantidade, p.Total)).Should().Equal(produtos);
    }

    [Then(@"^as últimas vendas começam pela mais recente, de (\d{2}/\d{2}/\d{4})$")]
    public void EntaoAsUltimasVendasComecamPelaMaisRecente(string data)
    {
        _painel!.UltimasVendas.Should().HaveCount(4);
        _painel.UltimasVendas[0].Data.Should().Be(Cenario.Data(data));
    }
}
