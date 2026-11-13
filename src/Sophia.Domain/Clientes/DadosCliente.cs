namespace Sophia.Domain.Clientes;

public sealed record DadosCliente(
    string? Cpf,
    string? Nome,
    DateOnly? DataNascimento,
    string? Email,
    string? Telefone);
