namespace Sophia.Domain.Vendas;

public sealed class ItemVenda
{
    private ItemVenda()
    {
        NomeDoProduto = null!;
    }

    internal ItemVenda(int produtoId, string nomeDoProduto, int quantidade, decimal valorUnitario)
    {
        ProdutoId = produtoId;
        NomeDoProduto = nomeDoProduto;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
    }

    public int Id { get; }

    public int VendaId { get; }

    public int ProdutoId { get; private set; }

    public string NomeDoProduto { get; private set; }

    public int Quantidade { get; private set; }

    public decimal ValorUnitario { get; private set; }

    public decimal Subtotal => Quantidade * ValorUnitario;
}
