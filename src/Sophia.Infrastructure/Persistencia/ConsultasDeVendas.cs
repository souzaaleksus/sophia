using Microsoft.EntityFrameworkCore;
using Sophia.Application;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia;

public sealed class ConsultasDeVendas(SophiaDbContext contexto) : IConsultasDeVendas
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
        var doPeriodo = contexto.Vendas.DoPeriodo(inicio, fim);
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
        contexto.VendaItens
            .Where(item => item.VendaId == vendaId)
            .OrderBy(item => item.Id)
            .Select(item => new ItemVendaDto(item.NomeDoProduto, item.Quantidade, item.ValorUnitario, item.Quantidade * item.ValorUnitario))
            .ToListAsync(cancelamento);

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

    private sealed class VendaComCliente
    {
        public required Venda Venda { get; init; }

        public required Cliente Cliente { get; init; }
    }

    private sealed class LinhaDeVenda
    {
        public int Id { get; init; }

        public DateOnly Data { get; init; }

        public required Cpf Cpf { get; init; }

        public required string Nome { get; init; }

        public int QuantidadeDeItens { get; init; }

        public decimal Total { get; init; }
    }
}
