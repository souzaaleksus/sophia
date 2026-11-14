using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sophia.Web.Apoio;

public static class DataIso
{
    public const string Formato = "yyyy-MM-dd";

    public static MvcOptions UsarDatasIso(this MvcOptions opcoes)
    {
        opcoes.ModelBinderProviders.Insert(0, new Provedor());
        return opcoes;
    }

    private sealed class Provedor : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
            context.Metadata.UnderlyingOrModelType == typeof(DateOnly) ? new Ligador() : null;
    }

    private sealed class Ligador : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valor = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valor != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valor);
                Ligar(bindingContext, valor.FirstValue);
            }

            return Task.CompletedTask;
        }

        private static void Ligar(ModelBindingContext contexto, string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                ExigirSeObrigatorio(contexto);
            }
            else if (DateOnly.TryParseExact(texto, Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            {
                contexto.Result = ModelBindingResult.Success(data);
            }
            else
            {
                Recusar(contexto, texto);
            }
        }

        private static void ExigirSeObrigatorio(ModelBindingContext contexto)
        {
            if (!contexto.ModelMetadata.IsReferenceOrNullableType)
            {
                var mensagem = contexto.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(contexto.ModelMetadata.GetDisplayName());
                contexto.ModelState.TryAddModelError(contexto.ModelName, mensagem);
            }
        }

        private static void Recusar(ModelBindingContext contexto, string texto)
        {
            var metadados = contexto.ModelMetadata;
            var mensagem = metadados.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor(texto, metadados.GetDisplayName());
            contexto.ModelState.TryAddModelError(contexto.ModelName, mensagem);
        }
    }
}
