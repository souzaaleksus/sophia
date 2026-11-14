using Sophia.Application.Produtos;
using Sophia.Domain.Produtos;

namespace Sophia.Tests.Fakes;

internal sealed class ProdutoRepositorioEmMemoria(BancoEmMemoria banco) : IProdutoRepositorio
{
    private IReadOnlyList<Produto> Gravados => banco.TabelaDeProdutos.Gravados;

    public Task<Produto?> ObterAsync(int id, CancellationToken cancelamento = default) =>
        Task.FromResult(banco.TabelaDeProdutos.Obter(id));

    public Task<IReadOnlyList<Produto>> ListarAsync(string? trechoDoNome, int? limite, CancellationToken cancelamento = default)
    {
        IEnumerable<Produto> consulta = Gravados;
        if (trechoDoNome is not null)
        {
            consulta = consulta.Where(p => p.Nome.Contains(trechoDoNome, StringComparison.OrdinalIgnoreCase));
        }

        consulta = consulta.OrderBy(p => p.Nome, StringComparer.CurrentCulture);
        if (limite is { } quantidade)
        {
            consulta = consulta.Take(quantidade);
        }

        return Task.FromResult<IReadOnlyList<Produto>>(consulta.ToList());
    }

    public Task<IReadOnlySet<int>> FiltrarExistentesAsync(IEnumerable<int> ids, CancellationToken cancelamento = default)
    {
        var procurados = ids.ToHashSet();
        IReadOnlySet<int> existentes = Gravados.Select(p => p.Id).Where(procurados.Contains).ToHashSet();
        return Task.FromResult(existentes);
    }

    public Task<bool> ExisteComNomeAsync(string nome, int? excetoId, CancellationToken cancelamento = default) =>
        Task.FromResult(Gravados.Any(p => string.Equals(p.Nome, nome, StringComparison.OrdinalIgnoreCase) && p.Id != excetoId));

    public Task<bool> FoiVendidoAsync(int id, CancellationToken cancelamento = default) =>
        Task.FromResult(banco.TabelaDeVendas.Gravados.Any(v => v.Itens.Any(i => i.ProdutoId == id)));

    public void Adicionar(Produto produto) => banco.TabelaDeProdutos.Adicionar(produto);

    public void Remover(Produto produto) => banco.TabelaDeProdutos.Remover(produto);
}
