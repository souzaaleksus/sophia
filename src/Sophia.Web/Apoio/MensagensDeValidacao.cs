using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Sophia.Web.Apoio;

public static class MensagensDeValidacao
{
    public const string ValorInvalido = "Valor inválido. Use 12,50 ou 1.250,00.";

    public static void TraduzirParaPortugues(DefaultModelBindingMessageProvider mensagens)
    {
        TraduzirValoresInvalidos(mensagens);
        TraduzirNumerosInvalidos(mensagens);
        TraduzirValoresAusentes(mensagens);
    }

    private static void TraduzirValoresInvalidos(DefaultModelBindingMessageProvider mensagens)
    {
        mensagens.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"O valor \"{valor}\" não é válido para {campo}.");
        mensagens.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"O valor \"{valor}\" não é válido.");
        mensagens.SetUnknownValueIsInvalidAccessor(campo => $"O valor informado não é válido para {campo}.");
        mensagens.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor informado não é válido.");
        mensagens.SetValueIsInvalidAccessor(valor => $"O valor \"{valor}\" não é válido.");
    }

    private static void TraduzirNumerosInvalidos(DefaultModelBindingMessageProvider mensagens)
    {
        mensagens.SetValueMustBeANumberAccessor(campo => $"Informe um número em {campo}.");
        mensagens.SetNonPropertyValueMustBeANumberAccessor(() => "Informe um número.");
    }

    private static void TraduzirValoresAusentes(DefaultModelBindingMessageProvider mensagens)
    {
        mensagens.SetValueMustNotBeNullAccessor(campo => $"Informe {campo}.");
        mensagens.SetMissingBindRequiredValueAccessor(campo => $"Informe {campo}.");
        mensagens.SetMissingKeyOrValueAccessor(() => "Informe um valor.");
        mensagens.SetMissingRequestBodyRequiredValueAccessor(() => "A requisição veio vazia.");
    }
}
