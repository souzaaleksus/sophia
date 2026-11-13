namespace Sophia.Application.Vendas;

public sealed record VendaResumoDto(
    int Id,
    DateOnly Data,
    string ClienteCpf,
    string ClienteNome,
    int QuantidadeDeItens,
    decimal Total);

public sealed record VendaDetalheDto(
    int Id,
    DateOnly Data,
    string ClienteCpf,
    string ClienteNome,
    IReadOnlyList<ItemVendaDto> Itens,
    decimal Total);

public sealed record ItemVendaDto(
    string Produto,
    int Quantidade,
    decimal ValorUnitario,
    decimal Subtotal);
