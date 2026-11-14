using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Sophia.Application.Abstracoes;

namespace Sophia.Infrastructure.Persistencia;

public sealed partial class UnidadeDeTrabalho(SophiaDbContext contexto) : IUnidadeDeTrabalho
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
        catch (DbUpdateException erro) when (erro.InnerException is SqlException sql && Conflito(sql, erro) is { } tipo)
        {
            contexto.ChangeTracker.Clear();
            throw new ConflitoAoSalvarException(tipo, erro);
        }
    }

    private TipoDeConflito? Conflito(SqlException sql, DbUpdateException erro) => sql.Number switch
    {
        ViolacaoDeIndiceUnico or ViolacaoDeRestricaoUnica => TipoDeConflito.ValorDuplicado,
        ViolacaoDeRestricao => ConflitoDeRestricao(RestricoesDoModelo.Violada(contexto.GetService<IDesignTimeModel>().Model, sql.Message), erro),
        _ => null,
    };

    private static TipoDeConflito? ConflitoDeRestricao(TipoDeRestricao? restricao, DbUpdateException erro) => restricao switch
    {
        TipoDeRestricao.ChaveEstrangeira when ExcluiuAlgum(erro) => TipoDeConflito.RegistroEmUso,
        TipoDeRestricao.ChaveEstrangeira => TipoDeConflito.ReferenciaInexistente,
        _ => null,
    };

    private static bool ExcluiuAlgum(DbUpdateException erro) =>
        erro.Entries.Any(entrada => entrada.State == EntityState.Deleted);

    private enum TipoDeRestricao
    {
        ChaveEstrangeira,
        Verificacao,
    }

    private static partial class RestricoesDoModelo
    {
        public static TipoDeRestricao? Violada(IModel modelo, string mensagem)
        {
            var conhecidas = Conhecidas(modelo);
            return NomesCitados().Matches(mensagem)
                .Select(citado => citado.Groups["nome"].Value)
                .Where(conhecidas.ContainsKey)
                .Select(nome => (TipoDeRestricao?)conhecidas[nome])
                .FirstOrDefault();
        }

        private static Dictionary<string, TipoDeRestricao> Conhecidas(IModel modelo)
        {
            var entidades = modelo.GetEntityTypes().ToList();
            var chaves = entidades
                .SelectMany(entidade => entidade.GetForeignKeys())
                .Select(chave => (Nome: chave.GetConstraintName(), Tipo: TipoDeRestricao.ChaveEstrangeira));
            var verificacoes = entidades
                .SelectMany(entidade => entidade.GetCheckConstraints())
                .Select(restricao => (Nome: restricao.Name, Tipo: TipoDeRestricao.Verificacao));

            return chaves.Concat(verificacoes)
                .Where(restricao => restricao.Nome is not null)
                .ToDictionary(restricao => restricao.Nome!, restricao => restricao.Tipo, StringComparer.Ordinal);
        }

        [GeneratedRegex("\"(?<nome>[^\"]+)\"")]
        private static partial Regex NomesCitados();
    }
}
