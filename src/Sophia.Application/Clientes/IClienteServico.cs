using Sophia.Domain;
using Sophia.Domain.Clientes;

namespace Sophia.Application.Clientes;

public interface IClienteServico
{
    Task<Lista<ClienteDto>> ListarAsync(ConsultaDeLista consulta, CancellationToken cancelamento = default);

    Task<IReadOnlyList<ClienteDto>> BuscarAsync(string? termo, int limite, CancellationToken cancelamento = default);

    Task<ClienteDto?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<Resultado<int>> CadastrarAsync(DadosCliente dados, CancellationToken cancelamento = default);

    Task<Resultado> AlterarAsync(int id, DadosCliente dados, CancellationToken cancelamento = default);

    Task<Resultado> ExcluirAsync(int id, CancellationToken cancelamento = default);
}
