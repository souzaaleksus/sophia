namespace Sophia.Domain.Vendas;

public sealed class ItemVenda
{
    private ItemVenda()
    {
    }

    internal ItemVenda(int produtoId, int quantidade, decimal valorUnitario)
    {
        ProdutoId = produtoId;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
    }

    public int Id { get; private set; }

    public int VendaId { get; private set; }

    public int ProdutoId { get; private set; }

    public int Quantidade { get; private set; }

    public decimal ValorUnitario { get; private set; }

    public decimal Subtotal => Quantidade * ValorUnitario;
}
