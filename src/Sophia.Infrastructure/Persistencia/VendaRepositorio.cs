using Microsoft.EntityFrameworkCore;
using Sophia.Application;
using Sophia.Application.Painel;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia;

public sealed class VendaRepositorio(SophiaDbContext contexto) : IVendaRepositorio
{
    public async Task<IReadOnlyList<VendaResumoDto>> ListarAsync(int? limite = null, CancellationToken cancelamento = default)
    {
        var consulta = ResumosDasMaisRecentesParaAsMaisAntigas();
        if (limite is { } quantidade)
        {
            consulta = consulta.Take(quantidade);
        }

        return ParaDtos(await consulta.ToListAsync(cancelamento));
    }

    public async Task<Pagina<VendaResumoDto>> ListarPaginaAsync(
        int numero,
        int tamanho,
        CancellationToken cancelamento = default)
    {
        var total = await contexto.Vendas.CountAsync(cancelamento);
        var linhas = await ResumosDasMaisRecentesParaAsMaisAntigas()
            .Skip((numero - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancelamento);

        return new Pagina<VendaResumoDto>(ParaDtos(linhas), numero, tamanho, total);
    }

    public async Task<VendaDetalheDto?> ObterDetalheAsync(int id, CancellationToken cancelamento = default)
    {
        var cabecalho = await (
                from venda in contexto.Vendas
                join cliente in contexto.Clientes on venda.ClienteId equals cliente.Id
                where venda.Id == id
                select new { venda.Id, venda.Data, cliente.Cpf, cliente.Nome })
            .FirstOrDefaultAsync(cancelamento);

        if (cabecalho is null)
        {
            return null;
        }

        var itens = await (
                from item in contexto.VendaItens
                join produto in contexto.Produtos on item.ProdutoId equals produto.Id
                where item.VendaId == id
                orderby item.Id
                select new ItemVendaDto(
                    produto.Nome,
                    item.Quantidade,
                    item.ValorUnitario,
                    item.Quantidade * item.ValorUnitario))
            .ToListAsync(cancelamento);

        return new VendaDetalheDto(
            cabecalho.Id,
            cabecalho.Data,
            cabecalho.Cpf.Formatado,
            cabecalho.Nome,
            itens,
            itens.Sum(i => i.Subtotal));
    }

    public async Task<ResumoDoPeriodo> ResumirAsync(
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancelamento = default)
    {
        var doPeriodo = contexto.Vendas.Where(v => v.Data >= inicio && v.Data <= fim);
        var quantidade = await doPeriodo.CountAsync(cancelamento);
        var total = await doPeriodo
            .SelectMany(v => v.Itens)
            .SumAsync(i => (decimal?)(i.Quantidade * i.ValorUnitario), cancelamento);

        return new ResumoDoPeriodo(quantidade, total ?? 0m);
    }

    public async Task<IReadOnlyList<TotalDoDia>> TotaisPorDiaAsync(
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancelamento = default)
    {
        var totais = await (
                from item in contexto.VendaItens
                join venda in contexto.Vendas on item.VendaId equals venda.Id
                where venda.Data >= inicio && venda.Data <= fim
                group new { item.VendaId, Subtotal = item.Quantidade * item.ValorUnitario } by venda.Data into dia
                orderby dia.Key
                select new
                {
                    Dia = dia.Key,
                    Total = dia.Sum(i => i.Subtotal),
                    QuantidadeDeVendas = dia.Select(i => i.VendaId).Distinct().Count(),
                })
            .ToListAsync(cancelamento);

        return totais.Select(t => new TotalDoDia(t.Dia, t.Total, t.QuantidadeDeVendas)).ToList();
    }

    public async Task<IReadOnlyList<ProdutoMaisVendido>> MaisVendidosAsync(
        DateOnly inicio,
        DateOnly fim,
        int limite,
        CancellationToken cancelamento = default)
    {
        var maisVendidos = await (
                from item in contexto.VendaItens
                join venda in contexto.Vendas on item.VendaId equals venda.Id
                join produto in contexto.Produtos on item.ProdutoId equals produto.Id
                where venda.Data >= inicio && venda.Data <= fim
                group new { item.Quantidade, Subtotal = item.Quantidade * item.ValorUnitario }
                    by new { produto.Id, produto.Nome } into vendido
                select new
                {
                    vendido.Key.Id,
                    vendido.Key.Nome,
                    Quantidade = vendido.Sum(i => i.Quantidade),
                    Total = vendido.Sum(i => i.Subtotal),
                } into total
                orderby total.Quantidade descending, total.Total descending, total.Nome
                select total)
            .Take(limite)
            .ToListAsync(cancelamento);

        return maisVendidos.Select(p => new ProdutoMaisVendido(p.Id, p.Nome, p.Quantidade, p.Total)).ToList();
    }

    public Task<int?> ObterIdPelaChaveAsync(Guid chave, CancellationToken cancelamento = default) =>
        contexto.Vendas
            .Where(v => v.ChaveDeRegistro == chave)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync(cancelamento);

    public void Adicionar(Venda venda) => contexto.Vendas.Add(venda);

    private static List<VendaResumoDto> ParaDtos(IEnumerable<LinhaDeResumo> linhas) =>
        linhas
            .Select(v => new VendaResumoDto(v.Id, v.Data, v.Cpf.Formatado, v.Nome, v.QuantidadeDeItens, v.Total))
            .ToList();

    private IQueryable<LinhaDeResumo> ResumosDasMaisRecentesParaAsMaisAntigas() =>
        from venda in contexto.Vendas
        join cliente in contexto.Clientes on venda.ClienteId equals cliente.Id
        orderby venda.Data descending, venda.Id descending
        select new LinhaDeResumo(
            venda.Id,
            venda.Data,
            cliente.Cpf,
            cliente.Nome,
            venda.Itens.Count(),
            venda.Itens.Sum(i => i.Quantidade * i.ValorUnitario));

    private sealed record LinhaDeResumo(int Id, DateOnly Data, Cpf Cpf, string Nome, int QuantidadeDeItens, decimal Total);
}
