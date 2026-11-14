using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;

namespace Sophia.Web.Apoio;

public static class MensagensDeValidacao
{
    public static void TraduzirParaPortugues(DefaultModelBindingMessageProvider mensagens, IStringLocalizer textos)
    {
        TraduzirValoresInvalidos(mensagens, textos);
        TraduzirNumerosInvalidos(mensagens, textos);
        TraduzirValoresAusentes(mensagens, textos);
    }

    private static void TraduzirValoresInvalidos(DefaultModelBindingMessageProvider mensagens, IStringLocalizer textos)
    {
        mensagens.SetAttemptedValueIsInvalidAccessor((valor, campo) => textos["binder.valor-invalido-no-campo", valor, campo]);
        mensagens.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => textos["binder.valor-invalido", valor]);
        mensagens.SetUnknownValueIsInvalidAccessor(campo => textos["binder.valor-desconhecido-no-campo", campo]);
        mensagens.SetNonPropertyUnknownValueIsInvalidAccessor(() => textos["binder.valor-desconhecido"]);
        mensagens.SetValueIsInvalidAccessor(valor => textos["binder.valor-invalido", valor]);
    }

    private static void TraduzirNumerosInvalidos(DefaultModelBindingMessageProvider mensagens, IStringLocalizer textos)
    {
        mensagens.SetValueMustBeANumberAccessor(campo => textos["binder.numero-no-campo", campo]);
        mensagens.SetNonPropertyValueMustBeANumberAccessor(() => textos["binder.numero"]);
    }

    private static void TraduzirValoresAusentes(DefaultModelBindingMessageProvider mensagens, IStringLocalizer textos)
    {
        mensagens.SetValueMustNotBeNullAccessor(campo => textos["binder.informe-o-campo", campo]);
        mensagens.SetMissingBindRequiredValueAccessor(campo => textos["binder.informe-o-campo", campo]);
        mensagens.SetMissingKeyOrValueAccessor(() => textos["binder.informe-um-valor"]);
        mensagens.SetMissingRequestBodyRequiredValueAccessor(() => textos["binder.requisicao-vazia"]);
    }
}
