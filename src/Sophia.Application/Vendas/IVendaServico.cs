using Sophia.Domain;
using Sophia.Domain.Vendas;

namespace Sophia.Application.Vendas;

public interface IVendaServico
{
    DateOnly DataSugerida();

    Task<Lista<VendaResumoDto>> ListarAsync(ConsultaDeVendas consulta, CancellationToken cancelamento = default);

    Task<VendaDetalheDto?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<Resultado<int>> RegistrarAsync(DadosVenda dados, CancellationToken cancelamento = default);
}
