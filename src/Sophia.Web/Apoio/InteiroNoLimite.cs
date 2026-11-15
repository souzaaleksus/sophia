using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sophia.Web.Apoio;

public static partial class InteiroNoLimite
{
    public static MvcOptions UsarInteirosNoLimite(this MvcOptions opcoes)
    {
        opcoes.ModelBinderProviders.Insert(0, new Provedor());
        return opcoes;
    }

    [GeneratedRegex(@"^[+-]?[0-9]+$")]
    private static partial Regex SoDigitos();

    private sealed class Provedor : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
            context.Metadata.UnderlyingOrModelType == typeof(int) ? new Ligador() : null;
    }

    private sealed class Ligador : LigadorDeTexto
    {
        protected override object? Interpretar(ModelBindingContext contexto, string texto, out string? recusa)
        {
            var limpo = texto.Trim();
            if (int.TryParse(limpo, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var valor))
            {
                recusa = null;
                return valor;
            }

            recusa = SoDigitos().IsMatch(limpo) ? ForaDoLimite(contexto, texto, limpo.StartsWith('-')) : ValorInvalido(contexto, texto);
            return null;
        }
    }
}
