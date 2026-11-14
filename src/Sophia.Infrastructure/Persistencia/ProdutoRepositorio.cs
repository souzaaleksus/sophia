using Microsoft.EntityFrameworkCore;
using Sophia.Application;
using Sophia.Application.Produtos;
using Sophia.Domain.Produtos;

namespace Sophia.Infrastructure.Persistencia;

public sealed class ProdutoRepositorio(SophiaDbContext contexto) : IProdutoRepositorio
{
    public Task<Produto?> ObterAsync(int id, CancellationToken cancelamento = default) =>
        contexto.Produtos.FirstOrDefaultAsync(p => p.Id == id, cancelamento);

    public async Task<IReadOnlyList<Produto>> ListarAsync(
        string? trechoDoNome,
        int? limite,
        CancellationToken cancelamento = default)
    {
        IQueryable<Produto> consulta = Filtrar(trechoDoNome).OrderBy(p => p.Nome).ThenBy(p => p.Id);
        if (limite is { } quantidade)
        {
            consulta = consulta.Take(quantidade);
        }

        return await consulta.ToListAsync(cancelamento);
    }

    public async Task<Pagina<Produto>> ListarPaginaAsync(
        string? trechoDoNome,
        Ordenacao<ColunaDeProdutos> ordem,
        int numero,
        int tamanho,
        CancellationToken cancelamento = default)
    {
        var filtrados = Filtrar(trechoDoNome);
        var total = await filtrados.CountAsync(cancelamento);
        var ordenados = ordem.Coluna == ColunaDeProdutos.Valor
            ? filtrados.OrdenarPor(p => p.ValorUnitario, ordem.Decrescente)
            : filtrados.OrdenarPor(p => p.Nome, ordem.Decrescente);
        var itens = await ordenados
            .DepoisPor(p => p.Id, ordem.Decrescente)
            .Recortar(numero, tamanho)
            .ToListAsync(cancelamento);

        return new Pagina<Produto>(itens, numero, tamanho, total);
    }

    public async Task<IReadOnlySet<int>> FiltrarExistentesAsync(
        IEnumerable<int> ids,
        CancellationToken cancelamento = default)
    {
        var procurados = ids.Distinct().ToList();
        var existentes = await contexto.Produtos
            .Where(p => procurados.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancelamento);

        return existentes.ToHashSet();
    }

    public Task<bool> ExisteComNomeAsync(string nome, int? excetoId, CancellationToken cancelamento = default) =>
        contexto.Produtos.AnyAsync(p => p.Nome == nome && p.Id != excetoId, cancelamento);

    public Task<bool> FoiVendidoAsync(int id, CancellationToken cancelamento = default) =>
        contexto.VendaItens.AnyAsync(i => i.ProdutoId == id, cancelamento);

    public void Adicionar(Produto produto) => contexto.Produtos.Add(produto);

    public void Remover(Produto produto) => contexto.Produtos.Remove(produto);

    private IQueryable<Produto> Filtrar(string? trechoDoNome)
    {
        var consulta = contexto.Produtos.AsNoTracking();
        return trechoDoNome is null ? consulta : consulta.Where(p => p.Nome.Contains(trechoDoNome));
    }
}
