using Microsoft.EntityFrameworkCore;
using Sophia.Application.Vendas;
using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia;

public sealed class VendaRepositorio(SophiaDbContext contexto) : IVendaRepositorio
{
    public async Task<IReadOnlyList<VendaResumoDto>> ListarAsync(CancellationToken cancelamento = default)
    {
        var vendas = await (
                from venda in contexto.Vendas
                join cliente in contexto.Clientes on venda.ClienteId equals cliente.Id
                orderby venda.Data descending, venda.Id descending
                select new
                {
                    venda.Id,
                    venda.Data,
                    cliente.Cpf,
                    cliente.Nome,
                    QuantidadeDeItens = venda.Itens.Count(),
                    Total = venda.Itens.Sum(i => i.Quantidade * i.ValorUnitario),
                })
            .ToListAsync(cancelamento);

        return vendas
            .Select(v => new VendaResumoDto(v.Id, v.Data, v.Cpf.Formatado, v.Nome, v.QuantidadeDeItens, v.Total))
            .ToList();
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

    public void Adicionar(Venda venda) => contexto.Vendas.Add(venda);
}
