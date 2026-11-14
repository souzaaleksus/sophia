using Sophia.Application.Painel;
using Sophia.Domain.Vendas;

namespace Sophia.Application.Vendas;

public interface IVendaRepositorio
{
    Task<IReadOnlyList<VendaResumoDto>> ListarAsync(int? limite = null, CancellationToken cancelamento = default);

    Task<Pagina<VendaResumoDto>> ListarPaginaAsync(
        DateOnly? inicio,
        DateOnly? fim,
        Ordenacao<ColunaDeVendas> ordem,
        int numero,
        int tamanho,
        CancellationToken cancelamento = default);

    Task<VendaDetalheDto?> ObterDetalheAsync(int id, CancellationToken cancelamento = default);

    Task<ResumoDoPeriodo> ResumirAsync(DateOnly inicio, DateOnly fim, CancellationToken cancelamento = default);

    Task<IReadOnlyList<TotalDoDia>> TotaisPorDiaAsync(
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancelamento = default);

    Task<IReadOnlyList<ProdutoMaisVendido>> MaisVendidosAsync(
        DateOnly inicio,
        DateOnly fim,
        int limite,
        CancellationToken cancelamento = default);

    Task<int?> ObterIdPelaChaveAsync(Guid chave, CancellationToken cancelamento = default);

    void Adicionar(Venda venda);
}
