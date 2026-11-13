using Sophia.Domain;
using Sophia.Domain.Vendas;

namespace Sophia.Application.Vendas;

public interface IVendaServico
{
    DateOnly DataSugerida();

    Task<IReadOnlyList<VendaResumoDto>> ListarAsync(CancellationToken cancelamento = default);

    Task<VendaDetalheDto?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<Resultado<int>> RegistrarAsync(DadosVenda dados, CancellationToken cancelamento = default);
}
