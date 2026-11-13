namespace Sophia.Application.Painel;

public interface IPainelServico
{
    Task<PainelDto> ObterAsync(CancellationToken cancelamento = default);
}
