using Sophia.Domain;
using Sophia.Domain.Clientes;

namespace Sophia.Application.Clientes;

public sealed record BuscaDeCliente(string? TrechoDoNome, string? InicioDoCpf)
{
    public static BuscaDeCliente Interpretar(string? termo)
    {
        var aparado = Texto.Aparar(termo);
        if (aparado is null)
        {
            return new BuscaDeCliente(null, null);
        }

        var digitos = Cpf.SomenteDigitos(aparado);
        if (aparado.Any(char.IsLetter) || digitos.Length == 0)
        {
            return new BuscaDeCliente(aparado, null);
        }

        return new BuscaDeCliente(null, digitos);
    }
}
