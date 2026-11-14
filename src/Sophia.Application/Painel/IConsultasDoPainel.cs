namespace Sophia.Application.Painel;

public interface IConsultasDoPainel
{
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
}
