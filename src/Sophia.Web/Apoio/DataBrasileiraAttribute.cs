using System.ComponentModel.DataAnnotations;
using System.Globalization;

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
}
