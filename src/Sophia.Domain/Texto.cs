namespace Sophia.Domain;

public static class Texto
{
    public static string? Aparar(string? texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? null
            : string.Join(' ', texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static string SomenteDigitos(string? texto) =>
        texto is null ? string.Empty : new string(texto.Where(char.IsAsciiDigit).ToArray());
}
