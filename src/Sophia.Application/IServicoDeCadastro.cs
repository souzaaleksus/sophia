using Sophia.Domain;

namespace Sophia.Application;

public interface IServicoDeCadastro<TDto, in TDados>
    where TDto : class
{
    Task<Lista<TDto>> ListarAsync(ConsultaDeLista consulta, CancellationToken cancelamento = default);

    Task<IReadOnlyList<TDto>> BuscarAsync(string? termo, int limite, CancellationToken cancelamento = default);

    Task<TDto?> ObterAsync(int id, CancellationToken cancelamento = default);

    Task<Resultado<int>> CadastrarAsync(TDados dados, CancellationToken cancelamento = default);

    Task<Resultado> AlterarAsync(int id, TDados dados, CancellationToken cancelamento = default);

    Task<Resultado> ExcluirAsync(int id, CancellationToken cancelamento = default);
}
