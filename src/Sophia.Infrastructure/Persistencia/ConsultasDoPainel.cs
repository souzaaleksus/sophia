using Microsoft.EntityFrameworkCore;
using Sophia.Application.Painel;
using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia;

public sealed class ConsultasDoPainel(SophiaDbContext contexto) : IConsultasDoPainel
{
    public async Task<ResumoDoPeriodo> ResumirAsync(
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancelamento = default)
    {
        var doPeriodo = contexto.Vendas.DoPeriodo(inicio, fim);
        var quantidade = await doPeriodo.CountAsync(cancelamento);
        return new ResumoDoPeriodo(quantidade, await SomarItensAsync(doPeriodo, cancelamento));
    }

    public async Task<IReadOnlyList<TotalDoDia>> TotaisPorDiaAsync(
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancelamento = default)
    {
        var totais = await (
                from item in ItensDoPeriodo(inicio, fim)
                group item by item.Data into dia
                orderby dia.Key
                select new { Dia = dia.Key, Total = dia.Sum(i => i.Subtotal), Vendas = dia.Select(i => i.VendaId).Distinct().Count() })
            .ToListAsync(cancelamento);

        return totais.ConvertAll(t => new TotalDoDia(t.Dia, t.Total, t.Vendas));
    }

    public async Task<IReadOnlyList<ProdutoMaisVendido>> MaisVendidosAsync(
        DateOnly inicio,
        DateOnly fim,
        int limite,
        CancellationToken cancelamento = default)
    {
        var maisVendidos = await TotaisPorProduto(inicio, fim)
            .OrderByDescending(p => p.Quantidade)
            .ThenByDescending(p => p.Total)
            .ThenBy(p => p.Nome)
            .Take(limite)
            .ToListAsync(cancelamento);

        return maisVendidos.ConvertAll(p => new ProdutoMaisVendido(p.Id, p.Nome, p.Quantidade, p.Total));
    }

    private IQueryable<ItemDoPeriodo> ItensDoPeriodo(DateOnly inicio, DateOnly fim) =>
        from item in contexto.VendaItens
        join venda in contexto.Vendas.DoPeriodo(inicio, fim) on item.VendaId equals venda.Id
        select new ItemDoPeriodo
        {
            VendaId = item.VendaId,
            ProdutoId = item.ProdutoId,
            Data = venda.Data,
            Quantidade = item.Quantidade,
            Subtotal = item.Quantidade * item.ValorUnitario,
        };

    private static async Task<decimal> SomarItensAsync(IQueryable<Venda> vendas, CancellationToken cancelamento) =>
        await vendas.SelectMany(v => v.Itens).SumAsync(i => (decimal?)(i.Quantidade * i.ValorUnitario), cancelamento) ?? 0m;

    private IQueryable<TotalDoProduto> TotaisPorProduto(DateOnly inicio, DateOnly fim) =>
        from item in ItensDoPeriodo(inicio, fim)
        join produto in contexto.Produtos on item.ProdutoId equals produto.Id
        group item by new { produto.Id, produto.Nome } into vendido
        select new TotalDoProduto
        {
            Id = vendido.Key.Id,
            Nome = vendido.Key.Nome,
            Quantidade = vendido.Sum(i => (long)i.Quantidade),
            Total = vendido.Sum(i => i.Subtotal),
        };

    private sealed class ItemDoPeriodo
    {
        public int VendaId { get; init; }

        public int ProdutoId { get; init; }

        public DateOnly Data { get; init; }

        public int Quantidade { get; init; }

        public decimal Subtotal { get; init; }
    }

    private sealed class TotalDoProduto
    {
        public int Id { get; init; }

        public required string Nome { get; init; }

        public long Quantidade { get; init; }

        public decimal Total { get; init; }
    }
}
