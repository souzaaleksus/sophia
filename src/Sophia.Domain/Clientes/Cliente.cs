using System.Text.RegularExpressions;

namespace Sophia.Domain.Clientes;

public sealed partial class Cliente
{
    public const int TamanhoMaximoNome = 150;
    public const int TamanhoMaximoEmail = 254;

    private static readonly DateOnly DataNascimentoMinima = new(1900, 1, 1);

    private Cliente()
    {
        Cpf = null!;
        Nome = null!;
    }

    public int Id { get; private set; }

    public Cpf Cpf { get; private set; }

    public string Nome { get; private set; }

    public DateOnly? DataNascimento { get; private set; }

    public string? Email { get; private set; }

    public string? Telefone { get; private set; }

    public static Resultado<Cliente> Criar(DadosCliente dados, DateOnly hoje)
    {
        var cliente = new Cliente();
        var resultado = cliente.Alterar(dados, hoje);
        return resultado.Sucesso ? Resultado.Ok(cliente) : Resultado.Falha<Cliente>(resultado.Erros);
    }

    public Resultado Alterar(DadosCliente dados, DateOnly hoje)
    {
        var erros = new List<Erro>();

        var cpf = Cpf.Criar(dados.Cpf);
        erros.AddRange(cpf.Erros);
        var nome = RegrasComuns.ValidarNome(dados.Nome, TamanhoMaximoNome, erros);
        var dataNascimento = ValidarDataNascimento(dados.DataNascimento, hoje, erros);
        var email = ValidarEmail(dados.Email, erros);
        var telefone = ValidarTelefone(dados.Telefone, erros);

        if (erros.Count > 0)
        {
            return Resultado.Falha(erros);
        }

        Cpf = cpf.Valor;
        Nome = nome!;
        DataNascimento = dataNascimento;
        Email = email;
        Telefone = telefone;
        return Resultado.Ok();
    }

    private static DateOnly? ValidarDataNascimento(DateOnly? data, DateOnly hoje, List<Erro> erros)
    {
        if (data > hoje)
        {
            erros.Add(new Erro("DataNascimento", "A data de nascimento não pode estar no futuro."));
        }
        else if (data < DataNascimentoMinima)
        {
            erros.Add(new Erro("DataNascimento", "Informe uma data de nascimento válida."));
        }

        return data;
    }

    private static string? ValidarEmail(string? email, List<Erro> erros)
    {
        var aparado = Texto.Aparar(email);
        if (aparado is not null && (aparado.Length > TamanhoMaximoEmail || !FormatoDeEmail().IsMatch(aparado)))
        {
            erros.Add(new Erro("Email", "E-mail inválido."));
        }

        return aparado;
    }

    private static string? ValidarTelefone(string? telefone, List<Erro> erros)
    {
        if (Texto.Aparar(telefone) is not { } aparado)
        {
            return null;
        }

        var digitos = Texto.SomenteDigitos(aparado);
        if (!FormatoDeTelefone().IsMatch(aparado) || digitos.Length is not (10 or 11))
        {
            erros.Add(new Erro("Telefone", "Informe o telefone com DDD."));
        }

        return digitos;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatoDeEmail();

    [GeneratedRegex(@"^[\d\s()\-]+$")]
    private static partial Regex FormatoDeTelefone();
}
