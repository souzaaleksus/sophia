using Sophia.Application.Abstracoes;
using Sophia.Domain;

namespace Sophia.Application;

internal static class GravacaoDeCadastro
{
    public static async Task<Resultado> SalvarCadastroAsync(
        this IUnidadeDeTrabalho unidadeDeTrabalho,
        Erro seDuplicado,
        Erro seEmUso,
        CancellationToken cancelamento = default)
    {
        try
        {
            await unidadeDeTrabalho.SalvarAsync(cancelamento);
            return Resultado.Ok();
        }
        catch (ConflitoAoSalvarException conflito)
        {
            return Resultado.Falha(conflito.Tipo == TipoDeConflito.ValorDuplicado ? seDuplicado : seEmUso);
        }
    }
}
