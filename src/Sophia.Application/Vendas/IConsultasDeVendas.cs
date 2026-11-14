namespace Sophia.Application.Vendas;

public interface IConsultasDeVendas
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
}
