using Reqnroll;
using Sophia.Domain;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Specs.Apoio;

namespace Sophia.Specs.Passos;

[Binding]
public sealed class PassosComuns(Cenario cenario)
{
    [Given(@"^que hoje é (\d{2}/\d{2}/\d{4})$")]
    public void DadoQueHojeE(string data)
    {
        cenario.Hoje = Cenario.Data(data)!.Value;
    }

    [Given("que existe o cliente {string} com o CPF {string}")]
    public async Task DadoQueExisteOCliente(string nome, string cpf)
    {
        var resultado = await cenario.Clientes.CadastrarAsync(new DadosCliente(cpf, nome, null, null, null));
        resultado.Sucesso.Should().BeTrue();
    }

    [Given(@"^que existe o produto ""(.*)"" de R\$ (.*)$")]
    public async Task DadoQueExisteOProduto(string nome, string valor)
    {
        var resultado = await cenario.Produtos.CadastrarAsync(new DadosProduto(nome, Cenario.Valor(valor)));
        resultado.Sucesso.Should().BeTrue();
    }

    [Given("que o cliente {string} comprou {int} {string}")]
    public async Task DadoQueOClienteComprou(string cliente, int quantidade, string produto)
    {
        var comprador = await cenario.ClienteChamadoAsync(cliente);
        var item = await cenario.ProdutoChamadoAsync(produto);
        var dados = new DadosVenda(cenario.Hoje, comprador.Id, [new(item.Id, quantidade, item.ValorUnitario)]);

        var resultado = await cenario.Vendas.RegistrarAsync(dados);

        resultado.Sucesso.Should().BeTrue();
        cenario.UltimaVendaId = resultado.Valor;
    }

    [Then("o cadastro é aceito")]
    [Then("a alteração é aceita")]
    [Then("a exclusão é aceita")]
    public void EntaoAOperacaoEAceita()
    {
        cenario.UltimoResultado.Should().NotBeNull();
        cenario.UltimoResultado!.Erros.Should().BeEmpty();
    }

    [Then(@"^(?:o cadastro é recusado|a alteração é recusada|a venda é recusada) com a mensagem ""(.*)"" no campo ""(.*)""$")]
    public void EntaoAOperacaoERecusadaNoCampo(string mensagem, string campo)
    {
        cenario.UltimoResultado!.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro(campo, mensagem));
    }

    [Then("a exclusão é recusada com a mensagem {string}")]
    public void EntaoAExclusaoERecusada(string mensagem)
    {
        cenario.UltimoResultado!.Erros.Should().ContainSingle()
            .Which.Mensagem.Should().Be(mensagem);
    }
}
