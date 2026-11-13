namespace Sophia.Application.Abstracoes;

public interface IUnidadeDeTrabalho
{
    Task SalvarAsync(CancellationToken cancelamento = default);
}
