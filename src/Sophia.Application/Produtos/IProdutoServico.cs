using Sophia.Domain;
using Sophia.Domain.Produtos;

namespace Sophia.Application.Produtos;

public interface IProdutoServico
{
    Task<Lista<ProdutoDto>> ListarAsync(ConsultaDeLista consulta, CancellationToken cancelamento = default);

    Task<IReadOnlyList<ProdutoDto>> BuscarAsync(string? trechoDoNome, int limite, CancellationToken cancelamento = default);

    Task<ProdutoDto?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<Resultado<int>> CadastrarAsync(DadosProduto dados, CancellationToken cancelamento = default);

    Task<Resultado> AlterarAsync(int id, DadosProduto dados, CancellationToken cancelamento = default);

    Task<Resultado> ExcluirAsync(int id, CancellationToken cancelamento = default);
}
