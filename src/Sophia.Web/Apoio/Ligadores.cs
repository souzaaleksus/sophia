using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sophia.Web.Apoio;

public static partial class Ligadores
{
    public const string FormatoDeData = "yyyy-MM-dd";

    public static MvcOptions UsarDatasIso(this MvcOptions opcoes) => Usar(opcoes, typeof(DateOnly), LerData);

    public static MvcOptions UsarNumerosDoBrasil(this MvcOptions opcoes) =>
        Usar(Usar(opcoes, typeof(decimal), LerDecimal), typeof(int), LerInteiro);

    private static MvcOptions Usar(MvcOptions opcoes, Type tipo, Func<ModelBindingContext, string, (object? Valor, string? Recusa)> ler)
    {
        opcoes.ModelBinderProviders.Insert(0, new Provedor(tipo, new Ligador(ler)));
        return opcoes;
    }

    private static (object? Valor, string? Recusa) LerData(ModelBindingContext contexto, string texto) =>
        DateOnly.TryParseExact(texto, FormatoDeData, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? (data, null)
            : (null, ValorInvalido(contexto, texto));

    private static (object? Valor, string? Recusa) LerInteiro(ModelBindingContext contexto, string texto)
    {
        var limpo = texto.Trim();
        if (int.TryParse(limpo, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var valor))
        {
            return (valor, null);
        }

        return (null, SoDigitos().IsMatch(limpo) ? ForaDoLimite(limpo.StartsWith('-')) : ValorInvalido(contexto, texto));
    }

    private static (object? Valor, string? Recusa) LerDecimal(ModelBindingContext contexto, string texto)
    {
        var limpo = Espacos().Replace(texto, string.Empty);
        var negativo = limpo.StartsWith('-');
        var numero = Normalizar(negativo ? limpo[1..] : limpo);
        if (numero is null)
        {
            return (null, MensagensDeValidacao.ValorInvalido);
        }

        if (!decimal.TryParse(numero, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
        {
            return (null, ForaDoLimite(negativo));
        }

        return (negativo ? -valor : valor, null);
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

        return ComPontoDecimal().IsMatch(numero) || SoAlgarismos().IsMatch(numero) ? numero : null;
    }

    private static string ForaDoLimite(bool abaixo) =>
        abaixo ? MensagensDeValidacao.NumeroMenorQueOCampo : MensagensDeValidacao.NumeroMaiorQueOCampo;

    private static string ValorInvalido(ModelBindingContext contexto, string texto)
    {
        var metadados = contexto.ModelMetadata;
        return metadados.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor(texto, metadados.GetDisplayName());
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
    private static partial Regex SoAlgarismos();

    [GeneratedRegex(@"^[+-]?[0-9]+$")]
    private static partial Regex SoDigitos();

    private sealed class Provedor(Type tipo, IModelBinder ligador) : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
            context.Metadata.UnderlyingOrModelType == tipo ? ligador : null;
    }

    private sealed class Ligador(Func<ModelBindingContext, string, (object? Valor, string? Recusa)> ler) : IModelBinder
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

        private void Ligar(ModelBindingContext contexto, string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                ExigirSeObrigatorio(contexto);
                return;
            }

            var (lido, recusa) = ler(contexto, texto);
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
}
