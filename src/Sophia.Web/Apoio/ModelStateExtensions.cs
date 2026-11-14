using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sophia.Domain;
using Sophia.Web.Models;

namespace Sophia.Web.Apoio;

public static class ModelStateExtensions
{
    public static void AdicionarErros(this ModelStateDictionary modelState, IEnumerable<Erro> erros)
    {
        foreach (var erro in erros)
        {
            modelState.AddModelError(ChaveNaTela(erro), erro.Mensagem);
        }
    }

    public static string? PrimeiroErro(this ModelStateDictionary modelState, string campo) =>
        modelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0
            ? entrada.Errors[0].ErrorMessage
            : null;

    private static string ChaveNaTela(Erro erro) =>
        erro.Linha is { } linha ? $"{nameof(NovaVendaFormulario.Itens)}[{linha}].{erro.Campo}" : erro.Campo;
}
