using System.Globalization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Sophia.Web.Apoio;

public static class FiltrosDaLista
{
    public static IReadOnlyDictionary<string, string> De(ViewDataDictionary dados)
    {
        var filtros = new Dictionary<string, string>();
        Incluir(filtros, "busca", dados["Busca"] as string);
        Incluir(filtros, "ordem", dados["Ordem"] as string);
        Incluir(filtros, "de", DataDaConsulta(dados["De"] as DateOnly?));
        Incluir(filtros, "ate", DataDaConsulta(dados["Ate"] as DateOnly?));
        return filtros;
    }

    public static string? DataDaConsulta(DateOnly? data) =>
        data?.ToString(Ligadores.FormatoDeData, CultureInfo.InvariantCulture);

    private static void Incluir(Dictionary<string, string> filtros, string chave, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
        {
            filtros[chave] = valor;
        }
    }
}
