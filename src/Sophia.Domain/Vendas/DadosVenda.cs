namespace Sophia.Domain.Vendas;

public sealed record DadosVenda(DateOnly? Data, int? ClienteId, IReadOnlyList<DadosItemVenda> Itens);

public sealed record DadosItemVenda(int? ProdutoId, int? Quantidade, decimal? ValorUnitario);
