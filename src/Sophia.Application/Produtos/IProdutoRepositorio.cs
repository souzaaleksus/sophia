using Sophia.Domain.Produtos;

namespace Sophia.Application.Produtos;

public interface IProdutoRepositorio
{
    Task<Produto?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<IReadOnlyList<Produto>> ListarAsync(string? trechoDoNome, int? limite, CancellationToken cancelamento = default);

    Task<Pagina<Produto>> ListarPaginaAsync(
        string? trechoDoNome,
        Ordenacao<ColunaDeProdutos> ordem,
        int numero,
        int tamanho,
        CancellationToken cancelamento = default);

    Task<IReadOnlyDictionary<int, string>> ObterNomesAsync(IEnumerable<int> ids, CancellationToken cancelamento = default);

    Task<bool> ExisteComNomeAsync(string nome, int? excetoId, CancellationToken cancelamento = default);

    Task<bool> FoiVendidoAsync(int id, CancellationToken cancelamento = default);

    void Adicionar(Produto produto);

    void Remover(Produto produto);
}
