using Sophia.Domain.Clientes;

namespace Sophia.Application.Clientes;

public interface IClienteRepositorio
{
    Task<Cliente?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<IReadOnlyList<Cliente>> ListarAsync(BuscaDeCliente busca, int? limite, CancellationToken cancelamento = default);

    Task<Pagina<Cliente>> ListarPaginaAsync(
        BuscaDeCliente busca,
        Ordenacao<ColunaDeClientes> ordem,
        int numero,
        int tamanho,
        CancellationToken cancelamento = default);

    Task<bool> ExisteAsync(int id, CancellationToken cancelamento = default);

    Task<bool> ExisteComCpfAsync(Cpf cpf, int? excetoId, CancellationToken cancelamento = default);

    Task<bool> PossuiVendasAsync(int id, CancellationToken cancelamento = default);

    void Adicionar(Cliente cliente);

    void Remover(Cliente cliente);
}
