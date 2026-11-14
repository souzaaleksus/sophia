using System.Text.RegularExpressions;
using Sophia.Domain.Recursos;

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

    public int Id { get; }

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
        var validos = Validar(dados, hoje, erros);
        if (erros.Count > 0)
        {
            return Resultado.Falha(erros);
        }

        Aplicar(validos);
        return Resultado.Ok();
    }

    private static DadosValidos Validar(DadosCliente dados, DateOnly hoje, List<Erro> erros)
    {
        var cpf = Cpf.Criar(dados.Cpf);
        erros.AddRange(cpf.Erros);
        return new DadosValidos(
            cpf.Sucesso ? cpf.Valor : null,
            RegrasComuns.ValidarNome(dados.Nome, nameof(DadosCliente.Nome), TamanhoMaximoNome, erros),
            ValidarDataNascimento(dados.DataNascimento, hoje, erros),
            ValidarEmail(dados.Email, erros),
            ValidarTelefone(dados.Telefone, erros));
    }

    private void Aplicar(DadosValidos validos)
    {
        Cpf = validos.Cpf!;
        Nome = validos.Nome!;
        DataNascimento = validos.DataNascimento;
        Email = validos.Email;
        Telefone = validos.Telefone;
    }

    private static DateOnly? ValidarDataNascimento(DateOnly? data, DateOnly hoje, List<Erro> erros)
    {
        if (data > hoje)
        {
            erros.Add(new Erro(nameof(DadosCliente.DataNascimento), Mensagens.DataDeNascimentoNoFuturo));
        }
        else if (data < DataNascimentoMinima)
        {
            erros.Add(new Erro(nameof(DadosCliente.DataNascimento), Mensagens.DataDeNascimentoInvalida));
        }

        return data;
    }

    private static string? ValidarEmail(string? email, List<Erro> erros)
    {
        var aparado = Texto.Aparar(email);
        if (aparado is not null && (aparado.Length > TamanhoMaximoEmail || !FormatoDeEmail().IsMatch(aparado)))
        {
            erros.Add(new Erro(nameof(DadosCliente.Email), Mensagens.EmailInvalido));
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
            erros.Add(new Erro(nameof(DadosCliente.Telefone), Mensagens.TelefoneSemDdd));
        }

        return digitos;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatoDeEmail();

    [GeneratedRegex(@"^[0-9\s()\-]+$")]
    private static partial Regex FormatoDeTelefone();

    private sealed record DadosValidos(Cpf? Cpf, string? Nome, DateOnly? DataNascimento, string? Email, string? Telefone);
}
