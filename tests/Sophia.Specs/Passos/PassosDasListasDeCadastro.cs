using Reqnroll;
using Sophia.Application;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Specs.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Specs.Passos;

[Binding]
public sealed class PassosDasListasDeCadastro(Cenario cenario)
{
    private Pagina<string>? _nomes;
    private string? _ordem;
    private decimal? _valorDoPrimeiro;

    [Given(@"^que estão cadastrados (\d+) clientes, de ""Cliente 01"" a ""Cliente \d+"", com os CPFs na ordem inversa à dos nomes$")]
    public async Task DadoQueEstaoCadastradosOsClientes(int quantidade)
    {
        for (var numero = 1; numero <= quantidade; numero++)
        {
            var dados = new DadosCliente(GeradorDeCpf.Gerar(quantidade - numero + 1), $"Cliente {numero:00}", null, null, null);
            (await cenario.Clientes.CadastrarAsync(dados)).Sucesso.Should().BeTrue();
        }
    }

    [Given(@"^que estão cadastrados (\d+) produtos, de ""Produto 01"" a ""Produto \d+"", com o valor caindo de R\$ [\d.,]+ a R\$ 1,00$")]
    public async Task DadoQueEstaoCadastradosOsProdutos(int quantidade)
    {
        for (var numero = 1; numero <= quantidade; numero++)
        {
            var dados = new DadosProduto($"Produto {numero:00}", quantidade - numero + 1);
            (await cenario.Produtos.CadastrarAsync(dados)).Sucesso.Should().BeTrue();
        }
    }

    [When(@"^eu consultar a página (-?\d+) dos (clientes|produtos)$")]
    public Task QuandoEuConsultarAPagina(int pagina, string lista) =>
        ConsultarAsync(lista, new ConsultaDeLista(pagina));

    [When(@"^eu consultar a página (-?\d+) dos (clientes|produtos) ordenada por ""(.*)""$")]
    public Task QuandoEuConsultarAPaginaOrdenada(int pagina, string lista, string ordem) =>
        ConsultarAsync(lista, new ConsultaDeLista(pagina, Ordem: ordem));

    [When(@"^eu buscar ""(.*)"" na lista de (clientes|produtos)$")]
    public Task QuandoEuBuscar(string busca, string lista) =>
        ConsultarAsync(lista, new ConsultaDeLista(Busca: busca));

    [When(@"^eu buscar ""(.*)"" na lista de (clientes|produtos) ordenada por ""(.*)""$")]
    public Task QuandoEuBuscarOrdenando(string busca, string lista, string ordem) =>
        ConsultarAsync(lista, new ConsultaDeLista(Busca: busca, Ordem: ordem));

    [When(@"^eu buscar na lista de clientes o CPF de ""(.*)""$")]
    public async Task QuandoEuBuscarOCpf(string nome)
    {
        var cliente = await cenario.ClienteChamadoAsync(nome);
        await ConsultarAsync("clientes", new ConsultaDeLista(Busca: cliente.Cpf));
    }

    [Then(@"^vejo (\d+) (?:clientes?|produtos?), de ""(.*)"" a ""(.*)""$")]
    public void EntaoVejoOsNomes(int quantidade, string primeiro, string ultimo)
    {
        _nomes!.Itens.Should().HaveCount(quantidade);
        _nomes.Itens[0].Should().Be(primeiro);
        _nomes.Itens[^1].Should().Be(ultimo);
    }

    [Then(@"^nenhum (?:cliente|produto) é encontrado$")]
    public void EntaoNenhumEEncontrado()
    {
        _nomes!.Itens.Should().BeEmpty();
        _nomes.TotalDeItens.Should().Be(0);
    }

    [Then(@"^estou na página (\d+) de (\d+), com os (?:clientes|produtos) (\d+) a (\d+) de (\d+)$")]
    public void EntaoEstouNaPagina(int numero, int totalDePaginas, int primeiro, int ultimo, int total)
    {
        _nomes!.Numero.Should().Be(numero);
        _nomes.TotalDePaginas.Should().Be(totalDePaginas);
        _nomes.PrimeiroItem.Should().Be(primeiro);
        _nomes.UltimoItem.Should().Be(ultimo);
        _nomes.TotalDeItens.Should().Be(total);
    }

    [Then(@"^a lista de (?:clientes|produtos) está ordenada por ""(.*)""$")]
    public void EntaoAListaEstaOrdenadaPor(string ordem)
    {
        _ordem.Should().Be(ordem);
    }

    [Then("o primeiro produto custa {string}")]
    public void EntaoOPrimeiroProdutoCusta(string valor)
    {
        _valorDoPrimeiro.Should().Be(Cenario.Valor(valor));
    }

    private async Task ConsultarAsync(string lista, ConsultaDeLista consulta)
    {
        if (lista == "clientes")
        {
            var clientes = await cenario.Clientes.ListarAsync(consulta);
            _nomes = clientes.Pagina.Converter(c => c.Nome);
            _ordem = clientes.Ordem;
            return;
        }

        var produtos = await cenario.Produtos.ListarAsync(consulta);
        _nomes = produtos.Pagina.Converter(p => p.Nome);
        _ordem = produtos.Ordem;
        _valorDoPrimeiro = produtos.Pagina.Itens.Count > 0 ? produtos.Pagina.Itens[0].ValorUnitario : null;
    }
}
