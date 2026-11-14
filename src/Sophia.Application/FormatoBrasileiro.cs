namespace Sophia.Application;

public static class FormatoBrasileiro
{
    public static string? Telefone(string? digitos) => digitos?.Length switch
    {
        11 => $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}",
        10 => $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}",
        _ => digitos,
    };
}
