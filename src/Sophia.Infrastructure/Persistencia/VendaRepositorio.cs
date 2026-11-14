using Microsoft.EntityFrameworkCore;
using Sophia.Application.Vendas;
using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia;

public sealed class VendaRepositorio(SophiaDbContext contexto) : IVendaRepositorio
{
    public Task<Venda?> ObterPelaChaveAsync(Guid chave, CancellationToken cancelamento = default) =>
        contexto.Vendas
            .AsNoTracking()
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.ChaveDeRegistro == chave, cancelamento);

    public void Adicionar(Venda venda) => contexto.Vendas.Add(venda);
}
