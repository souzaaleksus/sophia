using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class MapeamentoContraOScriptTests(BancoDeTestes banco)
{
    [Fact]
    public async Task Os_indices_do_mapeamento_sao_os_do_script()
    {
        await using var contexto = banco.NovoContexto();

        var doScript = await banco.ConsultarAsync("""
            SELECT CONCAT(
                t.name, '.', i.name,
                ' unico=', CAST(i.is_unique AS INT),
                ' filtro=', ISNULL(i.filter_definition, ''),
                ' colunas=', (
                    SELECT STRING_AGG(c.name + IIF(ic.is_descending_key = 1, ' DESC', ''), ',')
                        WITHIN GROUP (ORDER BY ic.key_ordinal)
                    FROM sys.index_columns ic
                    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0))
            FROM sys.indexes i
            JOIN sys.tables t ON t.object_id = i.object_id
            WHERE i.is_primary_key = 0 AND i.name IS NOT NULL
            """);

        IndicesDoModelo(ModeloCompleto(contexto)).Should().BeEquivalentTo(doScript);
    }

    [Fact]
    public async Task As_restricoes_de_verificacao_do_mapeamento_sao_as_do_script()
    {
        await using var contexto = banco.NovoContexto();

        var doScript = await banco.ConsultarAsync("""
            SELECT CONCAT(t.name, '.', cc.name)
            FROM sys.check_constraints cc
            JOIN sys.tables t ON t.object_id = cc.parent_object_id
            """);

        RestricoesDoModelo(ModeloCompleto(contexto)).Should().BeEquivalentTo(doScript);
    }

    [Fact]
    public async Task O_agrupamento_das_colunas_de_texto_do_mapeamento_e_o_do_script()
    {
        await using var contexto = banco.NovoContexto();
        var padraoDoBanco = (await banco.ConsultarAsync(
            "SELECT CONVERT(NVARCHAR(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation'))")).Single();

        var doScript = await banco.ConsultarAsync("""
            SELECT CONCAT(t.name, '.', c.name, '=', c.collation_name)
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            WHERE c.collation_name IS NOT NULL
            """);

        AgrupamentosDoModelo(ModeloCompleto(contexto), padraoDoBanco).Should().BeEquivalentTo(doScript);
    }

    private static IModel ModeloCompleto(DbContext contexto) => contexto.GetService<IDesignTimeModel>().Model;

    private static IEnumerable<string> IndicesDoModelo(IModel modelo) =>
        modelo.GetEntityTypes().SelectMany(entidade => entidade.GetIndexes().Select(indice => DescreverIndice(entidade, indice)));

    private static IEnumerable<string> RestricoesDoModelo(IModel modelo) =>
        modelo.GetEntityTypes().SelectMany(entidade =>
            entidade.GetCheckConstraints().Select(restricao => $"{entidade.GetTableName()}.{restricao.Name}"));

    private static IEnumerable<string> AgrupamentosDoModelo(IModel modelo, string padraoDoBanco) =>
        modelo.GetEntityTypes().SelectMany(entidade =>
        {
            var tabela = StoreObjectIdentifier.Table(entidade.GetTableName()!, entidade.GetSchema());
            return entidade.GetProperties()
                .Where(propriedade => propriedade.GetColumnType(tabela) is { } tipo && tipo.Contains("char", StringComparison.OrdinalIgnoreCase))
                .Select(propriedade =>
                    $"{tabela.Name}.{propriedade.GetColumnName(tabela)}={propriedade.GetCollation() ?? padraoDoBanco}");
        });

    private static string DescreverIndice(IEntityType entidade, IIndex indice)
    {
        var tabela = StoreObjectIdentifier.Table(entidade.GetTableName()!, entidade.GetSchema());
        var colunas = indice.Properties.Select((propriedade, posicao) =>
            propriedade.GetColumnName(tabela) + (Descendente(indice, posicao) ? " DESC" : string.Empty));
        var filtro = indice.GetFilter() is { } condicao ? $"({condicao})" : string.Empty;

        return $"{tabela.Name}.{indice.GetDatabaseName()} unico={(indice.IsUnique ? 1 : 0)} filtro={filtro} colunas={string.Join(',', colunas)}";
    }

    private static bool Descendente(IIndex indice, int posicao) =>
        indice.IsDescending is { } descendentes && (descendentes.Count == 0 || descendentes[posicao]);
}
