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

    private static (string? Numero, bool Negativo) Separar(string texto)
    {
        var limpo = Espacos().Replace(texto, string.Empty);
        var negativo = limpo.StartsWith('-');
        return (Normalizar(negativo ? limpo[1..] : limpo), negativo);
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

    [GeneratedRegex(@"^([1-9][0-9]{0,2}(\.[0-9]{3})+|[0-9]+),[0-9]+$")]
    private static partial Regex ComVirgula();

    [GeneratedRegex(@"^[1-9][0-9]{0,2}(\.[0-9]{3})+$")]
    private static partial Regex ComMilhar();

    [GeneratedRegex(@"^[0-9]+\.[0-9]{1,2}$")]
    private static partial Regex ComPontoDecimal();

    [GeneratedRegex(@"^[0-9]+$")]
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
            var (numero, negativo) = Separar(texto);
            if (numero is null)
            {
                recusa = EscritaInvalida(contexto);
                return null;
            }

            if (!decimal.TryParse(numero, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
            {
                recusa = negativo ? MaiorQueZero(contexto) : AcimaDoPermitido(contexto);
                return null;
            }

            recusa = null;
            return negativo ? -valor : valor;
        }

        private static string EscritaInvalida(ModelBindingContext contexto)
        {
            var textos = TextosDe(contexto);
            return textos["validacao.valor-invalido"];
        }

        private static string AcimaDoPermitido(ModelBindingContext contexto)
        {
            var textos = TextosDe(contexto);
            return textos["binder.numero-acima-do-permitido-no-campo", contexto.ModelMetadata.GetDisplayName()];
        }
    }
}
