using Sophia.Domain;
using Sophia.Domain.Produtos;

namespace Sophia.Application.Produtos;

public interface IProdutoServico
{
    Task<IReadOnlyList<ProdutoDto>> ListarAsync(string? trechoDoNome, int? limite = null, CancellationToken cancelamento = default);

    Task<ProdutoDto?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<Resultado<int>> CadastrarAsync(DadosProduto dados, CancellationToken cancelamento = default);

    Task<Resultado> AlterarAsync(int id, DadosProduto dados, CancellationToken cancelamento = default);

    Task<Resultado> ExcluirAsync(int id, CancellationToken cancelamento = default);
}
