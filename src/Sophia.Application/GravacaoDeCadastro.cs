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
        catch (ConflitoAoSalvarException conflito) when (ErroDoCadastro(conflito.Tipo, seDuplicado, seEmUso) is { } erro)
        {
            return Resultado.Falha(erro);
        }
    }

    private static Erro? ErroDoCadastro(TipoDeConflito tipo, Erro seDuplicado, Erro seEmUso) => tipo switch
    {
        TipoDeConflito.ValorDuplicado => seDuplicado,
        TipoDeConflito.RegistroEmUso => seEmUso,
        _ => null,
    };
}
