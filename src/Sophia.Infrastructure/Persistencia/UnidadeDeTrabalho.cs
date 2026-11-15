using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sophia.Application.Abstracoes;

namespace Sophia.Infrastructure.Persistencia;

public sealed class UnidadeDeTrabalho(SophiaDbContext contexto) : IUnidadeDeTrabalho
{
    private const int ViolacaoDeIndiceUnico = 2601;
    private const int ViolacaoDeRestricaoUnica = 2627;
    private const int ViolacaoDeRestricao = 547;

    public async Task SalvarAsync(CancellationToken cancelamento = default)
    {
        try
        {
            await contexto.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException erro)
        {
            if (erro.InnerException is not SqlException sql || Conflito(sql, erro) is not { } tipo)
            {
                throw;
            }

            contexto.ChangeTracker.Clear();
            throw new ConflitoAoSalvarException(tipo, erro);
        }
    }

    private static TipoDeConflito? Conflito(SqlException sql, DbUpdateException erro) => sql.Number switch
    {
        ViolacaoDeIndiceUnico or ViolacaoDeRestricaoUnica => TipoDeConflito.ValorDuplicado,
        ViolacaoDeRestricao when ExcluiuAlgum(erro) => TipoDeConflito.RegistroEmUso,
        ViolacaoDeRestricao => TipoDeConflito.RestricaoViolada,
        _ => null,
    };

    private static bool ExcluiuAlgum(DbUpdateException erro) =>
        erro.Entries.Any(entrada => entrada.State == EntityState.Deleted);
}
