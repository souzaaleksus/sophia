using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Sophia.Web.Apoio;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DataBrasileiraAttribute : ValidationAttribute
{
    public const string Formato = "dd/MM/yyyy";

    public override bool IsValid(object? value) =>
        value is not string texto || string.IsNullOrWhiteSpace(texto) || Ler(texto) is not null;

    public static DateOnly? Ler(string? texto) =>
        DateOnly.TryParseExact(texto?.Trim(), Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : null;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (IsValid(value))
        {
            return ValidationResult.Success;
        }

        var textos = validationContext.GetService(typeof(IStringLocalizer<Textos>)) as IStringLocalizer<Textos>;
        var mensagem = textos?[ErrorMessageString].Value ?? ErrorMessageString;
        return new ValidationResult(mensagem, [validationContext.MemberName ?? string.Empty]);
    }
}
