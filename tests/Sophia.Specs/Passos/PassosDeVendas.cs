using Reqnroll;
using Sophia.Application.Vendas;
using Sophia.Domain.Vendas;
using Sophia.Specs.Apoio;

namespace Sophia.Specs.Passos;

[Binding]
public sealed class PassosDeVendas(Cenario cenario)
{
    private readonly Guid _chaveDoFormulario = Guid.NewGuid();
    private DateOnly _dataSugerida;
    private int[] _idsDosEnvios = [];

    [When("eu iniciar uma nova venda")]
    public void QuandoEuIniciarUmaNovaVenda()
    {
        _dataSugerida = cenario.Vendas.DataSugerida();
    }

    [When(@"^eu registrar uma venda para o cliente ""(.*)"" em (\d{2}/\d{2}/\d{4}) com os itens$")]
    public async Task QuandoEuRegistrarUmaVenda(string cliente, string data, DataTable itens)
    {
        var comprador = await cenario.ClienteChamadoAsync(cliente);
        await RegistrarAsync(Cenario.Data(data), comprador.Id, await LerItensAsync(itens));
    }

    [When(@"^eu enviar duas vezes a mesma venda para o cliente ""(.*)"" em (\d{2}/\d{2}/\d{4}) com os itens$")]
    public async Task QuandoEuEnviarDuasVezesAMesmaVenda(string cliente, string data, DataTable itens)
    {
        var comprador = await cenario.ClienteChamadoAsync(cliente);
        var dados = new DadosVenda(Cenario.Data(data), comprador.Id, await LerItensAsync(itens), Guid.NewGuid());

        var primeiro = await cenario.Vendas.RegistrarAsync(dados);
        var segundo = await cenario.Vendas.RegistrarAsync(dados);

        _idsDosEnvios = [primeiro.Valor, segundo.Valor];
    }

    [Given(@"^que eu registrei pelo mesmo formulário uma venda para o cliente ""(.*)"" em (\d{2}/\d{2}/\d{4}) com os itens$")]
    [When(@"^eu registrar pelo mesmo formulário uma venda para o cliente ""(.*)"" em (\d{2}/\d{2}/\d{4}) com os itens$")]
    public async Task QuandoEuRegistrarPeloMesmoFormulario(string cliente, string data, DataTable itens)
    {
        var comprador = await cenario.ClienteChamadoAsync(cliente);
        var resultado = await cenario.Vendas.RegistrarAsync(
            new DadosVenda(Cenario.Data(data), comprador.Id, await LerItensAsync(itens), _chaveDoFormulario));
        cenario.UltimoResultado = resultado;
        cenario.UltimaVendaId = resultado.Sucesso ? resultado.Valor : null;
    }

    [When(@"^eu registrar uma venda para o cliente ""(.*)"" em (\d{2}/\d{2}/\d{4}) sem itens$")]
    public async Task QuandoEuRegistrarUmaVendaSemItens(string cliente, string data)
    {
        var comprador = await cenario.ClienteChamadoAsync(cliente);
        await RegistrarAsync(Cenario.Data(data), comprador.Id, []);
    }

    [When(@"^eu registrar uma venda sem cliente em (\d{2}/\d{2}/\d{4}) com os itens$")]
    public async Task QuandoEuRegistrarUmaVendaSemCliente(string data, DataTable itens)
    {
        await RegistrarAsync(Cenario.Data(data), null, await LerItensAsync(itens));
    }

    [Then(@"^a data sugerida é (\d{2}/\d{2}/\d{4})$")]
    public void EntaoADataSugeridaE(string data)
    {
        _dataSugerida.Should().Be(Cenario.Data(data));
    }

    [Then("a venda é registrada")]
    public void EntaoAVendaERegistrada()
    {
        cenario.UltimoResultado!.Erros.Should().BeEmpty();
        cenario.UltimaVendaId.Should().BePositive();
    }

    [Then("os dois envios apontam para a mesma venda")]
    public void EntaoOsDoisEnviosApontamParaAMesmaVenda()
    {
        _idsDosEnvios.Should().HaveCount(2).And.OnlyContain(id => id == _idsDosEnvios[0]);
    }

    [Then(@"^existem? (\d+) vendas? registradas?$")]
    public async Task EntaoExistemVendasRegistradas(int quantidade)
    {
        (await cenario.Vendas.ListarAsync(new ConsultaDeVendas())).Pagina.TotalDeItens.Should().Be(quantidade);
    }

    [Then("a venda mostra o CPF {string} e o nome {string}")]
    public async Task EntaoAVendaMostraOCliente(string cpf, string nome)
    {
        var venda = await UltimaVendaAsync();
        venda.ClienteCpf.Should().Be(cpf);
        venda.ClienteNome.Should().Be(nome);
    }

    [Then("a venda tem os itens")]
    public async Task EntaoAVendaTemOsItens(DataTable esperados)
    {
        var venda = await UltimaVendaAsync();
        var itens = esperados.Rows.Select(linha => new ItemVendaDto(
            linha["Produto"],
            int.Parse(linha["Quantidade"], Cenario.PtBr),
            Cenario.Valor(linha["ValorUnitario"])!.Value,
            Cenario.Valor(linha["Subtotal"])!.Value));

        venda.Itens.Should().Equal(itens);
    }

    [Then("o total da venda é {string}")]
    [Then("a última venda continua com o total {string}")]
    public async Task EntaoOTotalDaVendaE(string total)
    {
        var venda = await UltimaVendaAsync();
        venda.Total.Should().Be(Cenario.Valor(total));
    }

    private async Task RegistrarAsync(DateOnly? data, int? clienteId, IReadOnlyList<DadosItemVenda> itens)
    {
        var resultado = await cenario.Vendas.RegistrarAsync(new DadosVenda(data, clienteId, itens, Guid.NewGuid()));
        cenario.UltimoResultado = resultado;
        cenario.UltimaVendaId = resultado.Sucesso ? resultado.Valor : null;
    }

    private async Task<List<DadosItemVenda>> LerItensAsync(DataTable tabela)
    {
        var itens = new List<DadosItemVenda>();
        foreach (var linha in tabela.Rows)
        {
            var produto = await cenario.ProdutoChamadoAsync(linha["Produto"]);
            itens.Add(new DadosItemVenda(
                produto.Id,
                int.Parse(linha["Quantidade"], Cenario.PtBr),
                Cenario.Valor(linha["ValorUnitario"])));
        }

        return itens;
    }

    private async Task<VendaDetalheDto> UltimaVendaAsync()
    {
        var venda = await cenario.Vendas.ObterAsync(cenario.UltimaVendaId!.Value);
        venda.Should().NotBeNull();
        return venda!;
    }
}
