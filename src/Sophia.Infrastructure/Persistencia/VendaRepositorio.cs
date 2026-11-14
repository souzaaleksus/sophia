using Microsoft.EntityFrameworkCore;
using Sophia.Application;
using Sophia.Application.Painel;
using Sophia.Application.Vendas;
using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia;

public sealed class VendaRepositorio(SophiaDbContext contexto) : IVendaRepositorio
{
    private static readonly Ordenacao<ColunaDeVendas> MaisRecentes = new(ColunaDeVendas.Data, Decrescente: true);

    public async Task<IReadOnlyList<VendaResumoDto>> ListarAsync(int? limite = null, CancellationToken cancelamento = default) =>
        await ResumirAsync(contexto.Vendas, MaisRecentes, 1, limite ?? int.MaxValue, cancelamento);

    public async Task<Pagina<VendaResumoDto>> ListarPaginaAsync(
        DateOnly? inicio,
        DateOnly? fim,
        Ordenacao<ColunaDeVendas> ordem,
        int numero,
        int tamanho,
        CancellationToken cancelamento = default)
    {
        var doPeriodo = DoPeriodo(inicio, fim);
        var total = await doPeriodo.CountAsync(cancelamento);
        var itens = await ResumirAsync(doPeriodo, ordem, numero, tamanho, cancelamento);
        return new Pagina<VendaResumoDto>(itens, numero, tamanho, total);
    }

    public async Task<VendaDetalheDto?> ObterDetalheAsync(int id, CancellationToken cancelamento = default)
    {
        var cabecalho = await CabecalhoDaVendaAsync(id, cancelamento);
        if (cabecalho is null)
        {
            return null;
        }

        var itens = await ItensDaVendaAsync(id, cancelamento);
        return new VendaDetalheDto(cabecalho.Id, cabecalho.Data, cabecalho.Cpf.Formatado, cabecalho.Nome, itens, itens.Sum(i => i.Subtotal));
    }

    private Task<LinhaDeVenda?> CabecalhoDaVendaAsync(int id, CancellationToken cancelamento) =>
        ComCliente(contexto.Vendas.Where(v => v.Id == id))
            .Select(v => new LinhaDeVenda { Id = v.Venda.Id, Data = v.Venda.Data, Cpf = v.Cliente.Cpf, Nome = v.Cliente.Nome })
            .FirstOrDefaultAsync(cancelamento);

    private Task<List<ItemVendaDto>> ItensDaVendaAsync(int vendaId, CancellationToken cancelamento) =>
        (from item in contexto.VendaItens
         join produto in contexto.Produtos on item.ProdutoId equals produto.Id
         where item.VendaId == vendaId
         orderby item.Id
         select new ItemVendaDto(produto.Nome, item.Quantidade, item.ValorUnitario, item.Quantidade * item.ValorUnitario))
        .ToListAsync(cancelamento);

    public async Task<ResumoDoPeriodo> ResumirAsync(
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancelamento = default)
    {
        var doPeriodo = DoPeriodo(inicio, fim);
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

    public Task<int?> ObterIdPelaChaveAsync(Guid chave, CancellationToken cancelamento = default) =>
        contexto.Vendas
            .Where(v => v.ChaveDeRegistro == chave)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync(cancelamento);

    public void Adicionar(Venda venda) => contexto.Vendas.Add(venda);

    private IQueryable<Venda> DoPeriodo(DateOnly? inicio, DateOnly? fim)
    {
        var vendas = contexto.Vendas.AsQueryable();
        if (inicio is { } de)
        {
            vendas = vendas.Where(v => v.Data >= de);
        }

        return fim is { } ate ? vendas.Where(v => v.Data <= ate) : vendas;
    }

    private IQueryable<ItemDoPeriodo> ItensDoPeriodo(DateOnly inicio, DateOnly fim) =>
        from item in contexto.VendaItens
        join venda in DoPeriodo(inicio, fim) on item.VendaId equals venda.Id
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

    private sealed class ItemDoPeriodo
    {
        public int VendaId { get; init; }

        public int ProdutoId { get; init; }

        public DateOnly Data { get; init; }

        public int Quantidade { get; init; }

        public decimal Subtotal { get; init; }
    }

    private async Task<List<VendaResumoDto>> ResumirAsync(
        IQueryable<Venda> vendas,
        Ordenacao<ColunaDeVendas> ordem,
        int numero,
        int tamanho,
        CancellationToken cancelamento)
    {
        var linhas = await Ordenar(ComCliente(vendas), ordem)
            .Recortar(numero, tamanho)
            .Select(v => new LinhaDeVenda
            {
                Id = v.Venda.Id,
                Data = v.Venda.Data,
                Cpf = v.Cliente.Cpf,
                Nome = v.Cliente.Nome,
                QuantidadeDeItens = v.Venda.Itens.Count,
                Total = v.Venda.Itens.Sum(i => i.Quantidade * i.ValorUnitario),
            })
            .ToListAsync(cancelamento);

        return linhas.ConvertAll(v => new VendaResumoDto(v.Id, v.Data, v.Cpf.Formatado, v.Nome, v.QuantidadeDeItens, v.Total));
    }

    private IQueryable<VendaComCliente> ComCliente(IQueryable<Venda> vendas) =>
        from venda in vendas
        join cliente in contexto.Clientes on venda.ClienteId equals cliente.Id
        select new VendaComCliente { Venda = venda, Cliente = cliente };

    private static IOrderedQueryable<VendaComCliente> Ordenar(IQueryable<VendaComCliente> vendas, Ordenacao<ColunaDeVendas> ordem)
    {
        var porColuna = ordem.Coluna == ColunaDeVendas.Total
            ? vendas.OrdenarPor(v => v.Venda.Itens.Sum(i => i.Quantidade * i.ValorUnitario), ordem.Decrescente)
            : vendas.OrdenarPor(v => v.Venda.Data, ordem.Decrescente);
        return porColuna.DepoisPor(v => v.Venda.Id, ordem.Decrescente);
    }

    private IQueryable<TotalDoProduto> TotaisPorProduto(DateOnly inicio, DateOnly fim) =>
        from item in ItensDoPeriodo(inicio, fim)
        join produto in contexto.Produtos on item.ProdutoId equals produto.Id
        group item by new { produto.Id, produto.Nome } into vendido
        select new TotalDoProduto
        {
            Id = vendido.Key.Id,
            Nome = vendido.Key.Nome,
            Quantidade = vendido.Sum(i => i.Quantidade),
            Total = vendido.Sum(i => i.Subtotal),
        };

    private sealed class VendaComCliente
    {
        public required Venda Venda { get; init; }

        public required Sophia.Domain.Clientes.Cliente Cliente { get; init; }
    }

    private sealed class LinhaDeVenda
    {
        public int Id { get; init; }

        public DateOnly Data { get; init; }

        public required Sophia.Domain.Clientes.Cpf Cpf { get; init; }

        public required string Nome { get; init; }

        public int QuantidadeDeItens { get; init; }

        public decimal Total { get; init; }
    }

    private sealed class TotalDoProduto
    {
        public int Id { get; init; }

        public required string Nome { get; init; }

        public int Quantidade { get; init; }

        public decimal Total { get; init; }
    }
}
