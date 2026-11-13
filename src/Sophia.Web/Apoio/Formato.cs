using System.Globalization;

namespace Sophia.Web.Apoio;

public static class Formato
{
    private static readonly CultureInfo PortuguesDoBrasil = CultureInfo.GetCultureInfo("pt-BR");

    public static string Moeda(decimal valor) => valor.ToString("C", PortuguesDoBrasil);

    public static string Numero(decimal? valor) => valor?.ToString("N2", PortuguesDoBrasil) ?? string.Empty;

    public static string Data(DateOnly? data) => data?.ToString("dd/MM/yyyy", PortuguesDoBrasil) ?? string.Empty;

    public static string DataParaCampo(DateOnly? data) => data?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
}
