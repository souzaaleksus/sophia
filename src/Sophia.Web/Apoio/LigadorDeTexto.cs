using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sophia.Application;
using Sophia.Domain.Recursos;
using Sophia.Web.Models;

namespace Sophia.Web.Apoio;

internal abstract class LigadorDeTexto : IModelBinder
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

    protected abstract object? Interpretar(ModelBindingContext contexto, string texto, out string? recusa);

    protected static string ValorInvalido(ModelBindingContext contexto, string texto)
    {
        var metadados = contexto.ModelMetadata;
        return metadados.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor(texto, metadados.GetDisplayName());
    }

    protected static string ForaDoLimite(ModelBindingContext contexto, string texto, bool abaixo) =>
        (contexto.ModelMetadata.Name, abaixo) switch
        {
            (nameof(ItemDaVendaFormulario.Quantidade), true) => Mensagens.QuantidadeNaoPositiva,
            (nameof(ItemDaVendaFormulario.Quantidade), false) => Mensagens.QuantidadeAcimaDoMaximo(
                contexto.HttpContext.RequestServices.GetRequiredService<ParametrosDoSophia>().QuantidadeMaximaPorItem),
            (nameof(ItemDaVendaFormulario.ValorUnitario), true) => Mensagens.ValorUnitarioNaoPositivo,
            (nameof(ItemDaVendaFormulario.ValorUnitario), false) => Mensagens.ValorUnitarioAcimaDoPermitido,
            _ => ValorInvalido(contexto, texto),
        };

    private void Ligar(ModelBindingContext contexto, string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            ExigirSeObrigatorio(contexto);
            return;
        }

        var lido = Interpretar(contexto, texto, out var recusa);
        if (recusa is null)
        {
            contexto.Result = ModelBindingResult.Success(lido);
        }
        else
        {
            contexto.ModelState.TryAddModelError(contexto.ModelName, recusa);
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
}
