using Reqnroll;
using Sophia.Application;
using Sophia.Domain.Produtos;
using Sophia.Specs.Apoio;

namespace Sophia.Specs.Passos;

[Binding]
public sealed class PassosDeProdutos(Cenario cenario)
{
    [When("eu cadastrar o produto {string} com valor unitário de {string}")]
    public async Task QuandoEuCadastrarOProduto(string nome, string valor)
    {
        var dados = new DadosProduto(Cenario.Texto(nome), Cenario.Valor(valor));
        cenario.UltimoResultado = await cenario.Produtos.CadastrarAsync(dados);
    }

    [When("eu alterar o valor do produto {string} para {string}")]
    public async Task QuandoEuAlterarOValor(string nome, string valor)
    {
        var produto = await cenario.ProdutoChamadoAsync(nome);
        var dados = new DadosProduto(produto.Nome, Cenario.Valor(valor));
        cenario.UltimoResultado = await cenario.Produtos.AlterarAsync(produto.Id, dados);
    }

    [When("eu renomear o produto {string} para {string}")]
    public async Task QuandoEuRenomearOProduto(string nome, string novoNome)
    {
        var produto = await cenario.ProdutoChamadoAsync(nome);
        var dados = new DadosProduto(novoNome, produto.ValorUnitario);
        cenario.UltimoResultado = await cenario.Produtos.AlterarAsync(produto.Id, dados);
    }

    [When("eu excluir o produto {string}")]
    public async Task QuandoEuExcluirOProduto(string nome)
    {
        var produto = await cenario.ProdutoChamadoAsync(nome);
        cenario.UltimoResultado = await cenario.Produtos.ExcluirAsync(produto.Id);
    }

    [Then("o produto {string} aparece na lista com o valor {string}")]
    public async Task EntaoOProdutoApareceNaLista(string nome, string valor)
    {
        var produtos = (await cenario.Produtos.ListarAsync(new ConsultaDeLista())).Pagina.Itens;
        produtos.Should().ContainSingle(p => p.Nome == nome)
            .Which.ValorUnitario.Should().Be(Cenario.Valor(valor));
    }
}
