using Reqnroll;
using Sophia.Application;
using Sophia.Application.Vendas;
using Sophia.Domain.Vendas;
using Sophia.Specs.Apoio;

namespace Sophia.Specs.Passos;

[Binding]
public sealed class PassosDaConsultaDeVendas(Cenario cenario)
{
    private Lista<VendaResumoDto>? _lista;

    private Pagina<VendaResumoDto> Pagina => _lista!.Pagina;

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

    [Given(@"^que em (\d{2}/\d{2}/\d{4}) a cliente ""(.*)"" comprou (\d+) ""(.*)""$")]
    public async Task DadoQueNaDataAClienteComprou(string data, string nome, int quantidade, string nomeDoProduto)
    {
        var cliente = await cenario.ClienteChamadoAsync(nome);
        var produto = await cenario.ProdutoChamadoAsync(nomeDoProduto);
        var dados = new DadosVenda(Cenario.Data(data), cliente.Id, [new(produto.Id, quantidade, produto.ValorUnitario)]);

        (await cenario.Vendas.RegistrarAsync(dados)).Sucesso.Should().BeTrue();
    }

    [When(@"^eu consultar a página (-?\d+) das vendas$")]
    public Task QuandoEuConsultarAPagina(int numero) => ConsultarAsync(new ConsultaDeVendas(numero));

    [When(@"^eu consultar a página (-?\d+) das vendas ordenada por ""(.*)""$")]
    public Task QuandoEuConsultarAPaginaOrdenada(int numero, string ordem) =>
        ConsultarAsync(new ConsultaDeVendas(numero, Ordem: ordem));

    [When(@"^eu consultar as vendas de (\d{2}/\d{2}/\d{4}) a (\d{2}/\d{2}/\d{4})$")]
    public Task QuandoEuConsultarOPeriodo(string de, string ate) =>
        ConsultarAsync(new ConsultaDeVendas(De: Cenario.Data(de), Ate: Cenario.Data(ate)));

    [When(@"^eu consultar as vendas a partir de (\d{2}/\d{2}/\d{4})$")]
    public Task QuandoEuConsultarAPartirDe(string de) => ConsultarAsync(new ConsultaDeVendas(De: Cenario.Data(de)));

    [When(@"^eu consultar as vendas até (\d{2}/\d{2}/\d{4})$")]
    public Task QuandoEuConsultarAte(string ate) => ConsultarAsync(new ConsultaDeVendas(Ate: Cenario.Data(ate)));

    [Then(@"^vejo (\d+) vendas, de (\d{2}/\d{2}/\d{4}) a (\d{2}/\d{2}/\d{4})$")]
    public void EntaoVejoAsVendas(int quantidade, string primeira, string ultima)
    {
        var datas = Pagina.Itens.Select(v => v.Data).ToList();
        datas.Should().HaveCount(quantidade);
        datas[0].Should().Be(Cenario.Data(primeira));
        datas[^1].Should().Be(Cenario.Data(ultima));
        if (datas[0] >= datas[^1])
        {
            datas.Should().BeInDescendingOrder();
        }
        else
        {
            datas.Should().BeInAscendingOrder();
        }
    }

    [Then(@"^estou na página (\d+) de (\d+), com as vendas (\d+) a (\d+) de (\d+)$")]
    public void EntaoEstouNaPagina(int numero, int totalDePaginas, int primeira, int ultima, int total)
    {
        Pagina.Numero.Should().Be(numero);
        Pagina.TotalDePaginas.Should().Be(totalDePaginas);
        Pagina.PrimeiroItem.Should().Be(primeira);
        Pagina.UltimoItem.Should().Be(ultima);
        Pagina.TotalDeItens.Should().Be(total);
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

    [Then(@"^a primeira venda é a de (\d{2}/\d{2}/\d{4}) com total de R\$ (.*)$")]
    public void EntaoAPrimeiraVendaE(string data, string total)
    {
        Pagina.Itens[0].Data.Should().Be(Cenario.Data(data));
        Pagina.Itens[0].Total.Should().Be(Cenario.Valor(total));
    }

    [Then("a lista de vendas está ordenada por {string}")]
    public void EntaoAListaDeVendasEstaOrdenadaPor(string ordem)
    {
        _lista!.Ordem.Should().Be(ordem);
    }

    private async Task ConsultarAsync(ConsultaDeVendas consulta)
    {
        _lista = await cenario.Vendas.ListarAsync(consulta);
    }

    private bool Existe(string qual) => qual == "anterior" ? Pagina.TemAnterior : Pagina.TemProxima;
}
