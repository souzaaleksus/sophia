using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sophia.Domain;

namespace Sophia.Web.Apoio;

public static class ModelStateExtensions
{
    public static void AdicionarErros(this ModelStateDictionary modelState, IEnumerable<Erro> erros)
    {
        foreach (var erro in erros)
        {
            modelState.AddModelError(erro.Campo, erro.Mensagem);
        }
    }

    public static string? PrimeiroErro(this ModelStateDictionary modelState, string campo) =>
        modelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0
            ? entrada.Errors[0].ErrorMessage
            : null;
}
