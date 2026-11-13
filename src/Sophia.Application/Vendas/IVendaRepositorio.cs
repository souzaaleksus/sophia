using Sophia.Domain.Vendas;

namespace Sophia.Application.Vendas;

public interface IVendaRepositorio
{
    Task<IReadOnlyList<VendaResumoDto>> ListarAsync(CancellationToken cancelamento = default);

    Task<VendaDetalheDto?> ObterDetalheAsync(int id, CancellationToken cancelamento = default);

    void Adicionar(Venda venda);
}
