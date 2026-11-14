using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;

namespace Sophia.Web.Apoio;

public static partial class InteiroNoLimite
{
    public static MvcOptions UsarInteirosNoLimite(this MvcOptions opcoes)
    {
        opcoes.ModelBinderProviders.Insert(0, new Provedor());
        return opcoes;
    }

    [GeneratedRegex(@"^[+-]?\d+$")]
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

            recusa = SoDigitos().IsMatch(limpo) ? ForaDoLimite(contexto, limpo.StartsWith('-')) : ValorInvalido(contexto, texto);
            return null;
        }

        private static string ForaDoLimite(ModelBindingContext contexto, bool abaixo)
        {
            var textos = contexto.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Textos>>();
            var campo = contexto.ModelMetadata.GetDisplayName();
            return abaixo
                ? textos["binder.numero-abaixo-do-minimo-no-campo", campo, int.MinValue.ToString("N0", Formato.Cultura)]
                : textos["binder.numero-acima-do-maximo-no-campo", campo, int.MaxValue.ToString("N0", Formato.Cultura)];
        }
    }
}
