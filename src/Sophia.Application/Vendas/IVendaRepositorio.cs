using Sophia.Domain.Vendas;

namespace Sophia.Application.Vendas;

public interface IVendaRepositorio
{
    Task<Venda?> ObterPelaChaveAsync(Guid chave, CancellationToken cancelamento = default);

    void Adicionar(Venda venda);
}
