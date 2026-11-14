using Sophia.Domain.Clientes;

namespace Sophia.Application.Clientes;

public sealed record BuscaDeCliente(string? TrechoDoNome, string? InicioDoCpf)
{
    public bool FiltraAlgo => TrechoDoNome is not null || InicioDoCpf is not null;

    public static BuscaDeCliente Interpretar(string? termo)
    {
        var aparado = termo?.Trim();
        if (string.IsNullOrEmpty(aparado))
        {
            return new BuscaDeCliente(null, null);
        }

        if (aparado.Any(char.IsLetter))
        {
            return new BuscaDeCliente(aparado, null);
        }

        var digitos = Cpf.SomenteDigitos(aparado);
        return new BuscaDeCliente(null, digitos.Length > 0 ? digitos : null);
    }
}
