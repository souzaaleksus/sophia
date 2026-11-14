using Sophia.Domain.Produtos;

namespace Sophia.Application.Produtos;

public sealed record ProdutoDto(int Id, string Nome, decimal ValorUnitario)
{
    public static ProdutoDto De(Produto produto) => new(produto.Id, produto.Nome, produto.ValorUnitario);
}
