using System.Globalization;

namespace Sophia.Web.Apoio;

public static class Formato
{
    private static readonly CultureInfo PortuguesDoBrasil = CultureInfo.GetCultureInfo("pt-BR");

    public static string Moeda(decimal valor) => valor.ToString("C", PortuguesDoBrasil);

    public static string Numero(decimal? valor) => valor?.ToString("N2", PortuguesDoBrasil) ?? string.Empty;

    public static string Data(DateOnly? data) => data?.ToString("dd/MM/yyyy", PortuguesDoBrasil) ?? string.Empty;

    public static string DataPorExtenso(DateOnly data) =>
        PrimeiraMaiuscula(data.ToString("dddd, d 'de' MMMM 'de' yyyy", PortuguesDoBrasil));

    public static string NomeDoMes(DateOnly data) => data.ToString("MMMM", PortuguesDoBrasil);

    public static string Quantidade(int quantidade, string singular, string plural) =>
        $"{quantidade.ToString("N0", PortuguesDoBrasil)} {(quantidade == 1 ? singular : plural)}";

    public static string DataParaCampo(DateOnly? data) => data?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string PrimeiraMaiuscula(string texto) =>
        texto.Length == 0 ? texto : string.Concat(char.ToUpper(texto[0], PortuguesDoBrasil), texto[1..]);
}
