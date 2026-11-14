using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sophia.Web.Apoio;

public static class ValorTentado
{
    public static string Ou(ModelStateDictionary estado, string campo, string formatado) =>
        estado.TryGetValue(campo, out var entrada)
            && entrada.ValidationState == ModelValidationState.Invalid
            && entrada.AttemptedValue is { } tentado
                ? tentado
                : formatado;
}
