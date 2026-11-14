using Sophia.Application;

namespace Sophia.Web.Models;

public sealed record Paginador(
    int Numero,
    int TotalDePaginas,
    int PrimeiroItem,
    int UltimoItem,
    int TotalDeItens,
    IReadOnlyDictionary<string, string> Filtros)
{
    public bool TemAnterior => Numero > 1;

    public bool TemProxima => Numero < TotalDePaginas;

    public static Paginador De<T>(Pagina<T> pagina, IReadOnlyDictionary<string, string>? filtros = null) =>
        new(
            pagina.Numero,
            pagina.TotalDePaginas,
            pagina.PrimeiroItem,
            pagina.UltimoItem,
            pagina.TotalDeItens,
            filtros ?? new Dictionary<string, string>());

    public Dictionary<string, string> RotaDaPagina(int numero) =>
        new(Filtros) { ["pagina"] = numero.ToString(System.Globalization.CultureInfo.InvariantCulture) };
}
