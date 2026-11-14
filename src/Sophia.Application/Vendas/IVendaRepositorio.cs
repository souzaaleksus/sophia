using Sophia.Application.Painel;
using Sophia.Domain.Vendas;

namespace Sophia.Application.Vendas;

public interface IVendaRepositorio
{
    Task<IReadOnlyList<VendaResumoDto>> ListarAsync(int? limite = null, CancellationToken cancelamento = default);

    Task<VendaDetalheDto?> ObterDetalheAsync(int id, CancellationToken cancelamento = default);

    Task<ResumoDoPeriodo> ResumirAsync(DateOnly inicio, DateOnly fim, CancellationToken cancelamento = default);

    Task<int?> ObterIdPelaChaveAsync(Guid chave, CancellationToken cancelamento = default);

    void Adicionar(Venda venda);
}
