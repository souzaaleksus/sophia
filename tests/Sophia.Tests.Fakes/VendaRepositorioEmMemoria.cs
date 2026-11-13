using Sophia.Application.Painel;
using Sophia.Application.Vendas;
using Sophia.Domain.Vendas;

namespace Sophia.Tests.Fakes;

internal sealed class VendaRepositorioEmMemoria(BancoEmMemoria banco) : IVendaRepositorio
{
    public Task<IReadOnlyList<VendaResumoDto>> ListarAsync(int? limite = null, CancellationToken cancelamento = default)
    {
        var clientes = banco.TabelaDeClientes.Gravados.ToDictionary(c => c.Id);
        IReadOnlyList<VendaResumoDto> vendas = banco.TabelaDeVendas.Gravados
            .OrderByDescending(v => v.Data)
            .ThenByDescending(v => v.Id)
            .Select(v => new VendaResumoDto(
                v.Id,
                v.Data,
                clientes[v.ClienteId].Cpf.Formatado,
                clientes[v.ClienteId].Nome,
                v.Itens.Count,
                v.Total))
            .Take(limite ?? int.MaxValue)
            .ToList();

        return Task.FromResult(vendas);
    }

    public Task<VendaDetalheDto?> ObterDetalheAsync(int id, CancellationToken cancelamento = default)
    {
        var venda = banco.TabelaDeVendas.Gravados.FirstOrDefault(v => v.Id == id);
        if (venda is null)
        {
            return Task.FromResult<VendaDetalheDto?>(null);
        }

        var cliente = banco.TabelaDeClientes.Gravados.Single(c => c.Id == venda.ClienteId);
        var produtos = banco.TabelaDeProdutos.Gravados.ToDictionary(p => p.Id);
        var itens = venda.Itens
            .Select(i => new ItemVendaDto(produtos[i.ProdutoId].Nome, i.Quantidade, i.ValorUnitario, i.Subtotal))
            .ToList();

        return Task.FromResult<VendaDetalheDto?>(
            new VendaDetalheDto(venda.Id, venda.Data, cliente.Cpf.Formatado, cliente.Nome, itens, venda.Total));
    }

    public Task<ResumoDoPeriodo> ResumirAsync(DateOnly inicio, DateOnly fim, CancellationToken cancelamento = default)
    {
        var doPeriodo = banco.TabelaDeVendas.Gravados.Where(v => v.Data >= inicio && v.Data <= fim).ToList();
        return Task.FromResult(new ResumoDoPeriodo(doPeriodo.Count, doPeriodo.Sum(v => v.Total)));
    }

    public void Adicionar(Venda venda) => banco.TabelaDeVendas.Adicionar(venda);
}
