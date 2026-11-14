using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sophia.Application.Abstracoes;

namespace Sophia.Infrastructure.Persistencia;

public sealed class UnidadeDeTrabalho(SophiaDbContext contexto) : IUnidadeDeTrabalho
{
    private const int ViolacaoDeIndiceUnico = 2601;
    private const int ViolacaoDeRestricaoUnica = 2627;
    private const int ViolacaoDeRestricao = 547;
    private const string PrefixoDeChaveEstrangeira = "\"FK_";

    public async Task SalvarAsync(CancellationToken cancelamento = default)
    {
        try
        {
            await contexto.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException erro) when (erro.InnerException is SqlException sql && Conflito(sql, erro) is { } tipo)
        {
            contexto.ChangeTracker.Clear();
            throw new ConflitoAoSalvarException(tipo, erro);
        }
    }

    private static TipoDeConflito? Conflito(SqlException sql, DbUpdateException erro) => sql.Number switch
    {
        ViolacaoDeIndiceUnico or ViolacaoDeRestricaoUnica => TipoDeConflito.ValorDuplicado,
        ViolacaoDeRestricao when !ViolouChaveEstrangeira(sql) => null,
        ViolacaoDeRestricao when ExcluiuAlgum(erro) => TipoDeConflito.RegistroEmUso,
        ViolacaoDeRestricao => TipoDeConflito.ReferenciaInexistente,
        _ => null,
    };

    private static bool ViolouChaveEstrangeira(SqlException sql) =>
        sql.Message.Contains(PrefixoDeChaveEstrangeira, StringComparison.Ordinal);

    private static bool ExcluiuAlgum(DbUpdateException erro) =>
        erro.Entries.Any(entrada => entrada.State == EntityState.Deleted);
}
