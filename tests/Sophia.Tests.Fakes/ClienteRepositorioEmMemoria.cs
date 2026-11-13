using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;

namespace Sophia.Tests.Fakes;

internal sealed class ClienteRepositorioEmMemoria(BancoEmMemoria banco) : IClienteRepositorio
{
    private IReadOnlyList<Cliente> Gravados => banco.TabelaDeClientes.Gravados;

    public Task<Cliente?> ObterAsync(int id, CancellationToken cancelamento = default) =>
        Task.FromResult(banco.TabelaDeClientes.Obter(id));

    public Task<IReadOnlyList<Cliente>> ListarAsync(BuscaDeCliente busca, int? limite, CancellationToken cancelamento = default)
    {
        IEnumerable<Cliente> consulta = Gravados;
        if (busca.TrechoDoNome is { } trecho)
        {
            consulta = consulta.Where(c => c.Nome.Contains(trecho, StringComparison.OrdinalIgnoreCase));
        }

        if (busca.InicioDoCpf is { } inicio)
        {
            consulta = consulta.Where(c => c.Cpf.Numero.StartsWith(inicio, StringComparison.Ordinal));
        }

        consulta = consulta.OrderBy(c => c.Nome, StringComparer.CurrentCulture);
        if (limite is { } quantidade)
        {
            consulta = consulta.Take(quantidade);
        }

        return Task.FromResult<IReadOnlyList<Cliente>>(consulta.ToList());
    }

    public Task<bool> ExisteAsync(int id, CancellationToken cancelamento = default) =>
        Task.FromResult(Gravados.Any(c => c.Id == id));

    public Task<bool> ExisteComCpfAsync(Cpf cpf, int? excetoId, CancellationToken cancelamento = default) =>
        Task.FromResult(Gravados.Any(c => c.Cpf == cpf && c.Id != excetoId));

    public Task<bool> PossuiVendasAsync(int id, CancellationToken cancelamento = default) =>
        Task.FromResult(banco.TabelaDeVendas.Gravados.Any(v => v.ClienteId == id));

    public void Adicionar(Cliente cliente) => banco.TabelaDeClientes.Adicionar(cliente);

    public void Remover(Cliente cliente) => banco.TabelaDeClientes.Remover(cliente);
}
