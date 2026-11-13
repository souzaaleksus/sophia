namespace Sophia.Domain;

internal static class Texto
{
    public static string? Aparar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    public static string SomenteDigitos(string? texto) =>
        texto is null ? string.Empty : new string(texto.Where(char.IsAsciiDigit).ToArray());
}
