using Sophia.Domain.Clientes;

namespace Sophia.Application.Clientes;

public sealed record ClienteDto(
    int Id,
    string Cpf,
    string Nome,
    DateOnly? DataNascimento,
    string? Email,
    string? Telefone)
{
    public static ClienteDto De(Cliente cliente) => new(
        cliente.Id,
        cliente.Cpf.Formatado,
        cliente.Nome,
        cliente.DataNascimento,
        cliente.Email,
        FormatoBrasileiro.Telefone(cliente.Telefone));
}
