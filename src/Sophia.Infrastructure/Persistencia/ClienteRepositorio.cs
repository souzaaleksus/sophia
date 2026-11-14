using Microsoft.EntityFrameworkCore;
using Sophia.Application;
using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;

namespace Sophia.Infrastructure.Persistencia;

public sealed class ClienteRepositorio(SophiaDbContext contexto) : IClienteRepositorio
{
    public Task<Cliente?> ObterAsync(int id, CancellationToken cancelamento = default) =>
        contexto.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancelamento);

    public async Task<IReadOnlyList<Cliente>> ListarAsync(
        BuscaDeCliente busca,
        int? limite,
        CancellationToken cancelamento = default)
    {
        IQueryable<Cliente> consulta = Filtrar(busca).OrderBy(c => c.Nome).ThenBy(c => c.Id);
        if (limite is { } quantidade)
        {
            consulta = consulta.Take(quantidade);
        }

        return await consulta.ToListAsync(cancelamento);
    }

    public async Task<Pagina<Cliente>> ListarPaginaAsync(
        BuscaDeCliente busca,
        Ordenacao<ColunaDeClientes> ordem,
        int numero,
        int tamanho,
        CancellationToken cancelamento = default)
    {
        var filtrados = Filtrar(busca);
        var total = await filtrados.CountAsync(cancelamento);
        var ordenados = ordem.Coluna == ColunaDeClientes.Cpf
            ? filtrados.OrdenarPor(c => c.Cpf, ordem.Decrescente)
            : filtrados.OrdenarPor(c => c.Nome, ordem.Decrescente);
        var itens = await ordenados
            .DepoisPor(c => c.Id, ordem.Decrescente)
            .Recortar(numero, tamanho)
            .ToListAsync(cancelamento);

        return new Pagina<Cliente>(itens, numero, tamanho, total);
    }

    public Task<bool> ExisteAsync(int id, CancellationToken cancelamento = default) =>
        contexto.Clientes.AnyAsync(c => c.Id == id, cancelamento);

    public Task<bool> ExisteComCpfAsync(Cpf cpf, int? excetoId, CancellationToken cancelamento = default) =>
        contexto.Clientes.AnyAsync(c => c.Cpf == cpf && c.Id != excetoId, cancelamento);

    public Task<bool> PossuiVendasAsync(int id, CancellationToken cancelamento = default) =>
        contexto.Vendas.AnyAsync(v => v.ClienteId == id, cancelamento);

    public void Adicionar(Cliente cliente) => contexto.Clientes.Add(cliente);

    public void Remover(Cliente cliente) => contexto.Clientes.Remove(cliente);

    private IQueryable<Cliente> Filtrar(BuscaDeCliente busca)
    {
        var consulta = contexto.Clientes.AsNoTracking();

        if (busca.TrechoDoNome is { } trecho)
        {
            consulta = consulta.Where(c => c.Nome.Contains(trecho));
        }

        if (busca.InicioDoCpf is { } inicio)
        {
            consulta = consulta.Where(c => ((string)(object)c.Cpf).StartsWith(inicio));
        }

        return consulta;
    }
}
