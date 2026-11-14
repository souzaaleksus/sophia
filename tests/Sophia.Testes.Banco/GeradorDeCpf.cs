using System.Globalization;

namespace Sophia.Testes.Banco;

public static class GeradorDeCpf
{
    public static string Gerar(int sequencia)
    {
        var digitos = (100_000_000 + sequencia).ToString(CultureInfo.InvariantCulture);
        digitos += Verificador(digitos);
        return digitos + Verificador(digitos);
    }

    private static char Verificador(string anteriores)
    {
        var soma = 0;
        var peso = anteriores.Length + 1;
        foreach (var digito in anteriores)
        {
            soma += (digito - '0') * peso;
            peso--;
        }

        var resto = soma % 11;
        return (char)('0' + (resto < 2 ? 0 : 11 - resto));
    }
}
