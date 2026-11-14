namespace Sophia.Domain.Produtos;

public sealed class Produto
{
    public const int TamanhoMaximoNome = 150;

    private Produto()
    {
        Nome = null!;
    }

    public int Id { get; }

    public string Nome { get; private set; }

    public decimal ValorUnitario { get; private set; }

    public static Resultado<Produto> Criar(DadosProduto dados)
    {
        var produto = new Produto();
        var resultado = produto.Alterar(dados);
        return resultado.Sucesso ? Resultado.Ok(produto) : Resultado.Falha<Produto>(resultado.Erros);
    }

    public Resultado Alterar(DadosProduto dados)
    {
        var erros = new List<Erro>();
        var nome = RegrasComuns.ValidarNome(dados.Nome, TamanhoMaximoNome, erros);
        var valorUnitario = RegrasComuns.ValidarValorUnitario(dados.ValorUnitario, "ValorUnitario", erros);

        if (erros.Count > 0)
        {
            return Resultado.Falha(erros);
        }

        Nome = nome!;
        ValorUnitario = valorUnitario!.Value;
        return Resultado.Ok();
    }
}
