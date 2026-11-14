using Sophia.Domain;
using Sophia.Domain.Clientes;

namespace Sophia.Application.Clientes;

public interface IClienteServico
{
    Task<IReadOnlyList<ClienteDto>> ListarAsync(string? termo, int? limite = null, CancellationToken cancelamento = default);

    Task<ClienteDto?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<Resultado<int>> CadastrarAsync(DadosCliente dados, CancellationToken cancelamento = default);

    Task<Resultado> AlterarAsync(int id, DadosCliente dados, CancellationToken cancelamento = default);

    Task<Resultado> ExcluirAsync(int id, CancellationToken cancelamento = default);
}
