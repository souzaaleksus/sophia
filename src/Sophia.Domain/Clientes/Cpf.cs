using Sophia.Domain.Recursos;

namespace Sophia.Domain.Clientes;

public sealed record Cpf
{
    private const string Campo = "Cpf";
    private const int QuantidadeDeDigitos = 11;

    private Cpf(string numero)
    {
        Numero = numero;
    }

    public string Numero { get; }

    public string Formatado => $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..]}";

    public static Resultado<Cpf> Criar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return Resultado.Falha<Cpf>(new Erro(Campo, Mensagens.CpfObrigatorio));
        }

        var digitos = SomenteDigitos(valor);
        if (!ContemSomenteDigitosEMascara(valor) || !DigitosVerificadoresConferem(digitos))
        {
            return Resultado.Falha<Cpf>(new Erro(Campo, Mensagens.CpfInvalido));
        }

        return Resultado.Ok(new Cpf(digitos));
    }

    public static Cpf DoBanco(string digitos) => new(digitos);

    public static string SomenteDigitos(string? texto) => Texto.SomenteDigitos(texto);

    public override string ToString() => Formatado;

    private static bool ContemSomenteDigitosEMascara(string valor) =>
        valor.All(c => char.IsAsciiDigit(c) || c is '.' or '-' or ' ');

    private static bool DigitosVerificadoresConferem(string digitos)
    {
        if (digitos.Length != QuantidadeDeDigitos || digitos.All(d => d == digitos[0]))
        {
            return false;
        }

        return digitos[9] == CalcularDigitoVerificador(digitos[..9])
            && digitos[10] == CalcularDigitoVerificador(digitos[..10]);
    }

    private static char CalcularDigitoVerificador(string digitosAnteriores)
    {
        var soma = 0;
        var peso = digitosAnteriores.Length + 1;
        foreach (var digito in digitosAnteriores)
        {
            soma += (digito - '0') * peso;
            peso--;
        }

        var resto = soma % 11;
        var verificador = resto < 2 ? 0 : 11 - resto;
        return (char)('0' + verificador);
    }
}
