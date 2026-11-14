using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sophia.Web.Apoio;

public static partial class DecimalPtBr
{
    public static MvcOptions UsarDecimaisPtBr(this MvcOptions opcoes)
    {
        opcoes.ModelBinderProviders.Insert(0, new Provedor());
        return opcoes;
    }

    public static decimal? Ler(string? texto)
    {
        var limpo = Espacos().Replace(texto ?? string.Empty, string.Empty);
        var negativo = limpo.StartsWith('-');
        var numero = Normalizar(negativo ? limpo[1..] : limpo);
        if (numero is null || !decimal.TryParse(numero, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
        {
            return null;
        }

        return negativo ? -valor : valor;
    }

    private static string? Normalizar(string numero)
    {
        if (ComVirgula().IsMatch(numero))
        {
            return numero.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        }

        if (ComMilhar().IsMatch(numero))
        {
            return numero.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return ComPontoDecimal().IsMatch(numero) || Inteiro().IsMatch(numero) ? numero : null;
    }

    [GeneratedRegex(@"\s")]
    private static partial Regex Espacos();

    [GeneratedRegex(@"^([1-9]\d{0,2}(\.\d{3})+|\d+),\d+$")]
    private static partial Regex ComVirgula();

    [GeneratedRegex(@"^[1-9]\d{0,2}(\.\d{3})+$")]
    private static partial Regex ComMilhar();

    [GeneratedRegex(@"^\d+\.\d{1,2}$")]
    private static partial Regex ComPontoDecimal();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex Inteiro();

    private sealed class Provedor : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
            context.Metadata.UnderlyingOrModelType == typeof(decimal) ? new Ligador() : null;
    }

    private sealed class Ligador : LigadorDeTexto
    {
        protected override object? Interpretar(ModelBindingContext contexto, string texto, out string? recusa)
        {
            var valor = DecimalPtBr.Ler(texto);
            recusa = valor is null ? ValorInvalido(contexto, texto) : null;
            return valor;
        }
    }
}
