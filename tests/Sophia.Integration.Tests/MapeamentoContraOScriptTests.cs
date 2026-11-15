using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Sophia.Application;
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

    [Fact]
    public async Task As_chaves_estrangeiras_do_mapeamento_sao_as_do_script()
    {
        await using var contexto = banco.NovoContexto();

        var doScript = await banco.ConsultarAsync("SELECT name FROM sys.foreign_keys");

        ModeloCompleto(contexto).GetEntityTypes()
            .SelectMany(entidade => entidade.GetForeignKeys())
            .Select(chave => chave.GetConstraintName())
            .Should().BeEquivalentTo(doScript);
    }

    [Fact]
    public async Task O_teto_da_quantidade_no_mapeamento_e_no_banco_e_o_maximo_aceito_pelo_parametro()
    {
        await using var contexto = banco.NovoContexto();
        var maximoDoParametro = typeof(ParametrosDoSophia)
            .GetProperty(nameof(ParametrosDoSophia.QuantidadeMaximaPorItem))!
            .GetCustomAttribute<RangeAttribute>()!.Maximum;
        var teto = Convert.ToString(maximoDoParametro, CultureInfo.InvariantCulture);

        var doModelo = ModeloCompleto(contexto).GetEntityTypes()
            .SelectMany(entidade => entidade.GetCheckConstraints())
            .Single(restricao => restricao.Name == "CK_VendaItens_QuantidadeMaxima").Sql;
        var doScript = await banco.ConsultarAsync("""
            SELECT definition FROM sys.check_constraints WHERE name = N'CK_VendaItens_QuantidadeMaxima'
            """);

        doModelo.Should().Be($"Quantidade <= {teto}");
        doScript.Should().Equal($"([Quantidade]<=({teto}))");
    }

    [Theory]
    [InlineData("Produtos", "CK_Produtos_ValorUnitarioMaximo")]
    [InlineData("VendaItens", "CK_VendaItens_ValorUnitarioMaximo")]
    public async Task O_teto_do_valor_unitario_no_mapeamento_e_no_banco_e_o_maior_valor_aceito_pelo_dominio(string tabela, string restricao)
    {
        await using var contexto = banco.NovoContexto();
        var teto = RegrasDeEntrada.MaiorValorAceito.ToString(CultureInfo.InvariantCulture);

        var doModelo = ModeloCompleto(contexto).GetEntityTypes()
            .Where(entidade => entidade.GetTableName() == tabela)
            .SelectMany(entidade => entidade.GetCheckConstraints())
            .Single(verificacao => verificacao.Name == restricao).Sql;
        var doScript = await banco.ConsultarAsync($"SELECT definition FROM sys.check_constraints WHERE name = N'{restricao}'");

        teto.Should().Be("999999.99");
        doModelo.Should().Be($"ValorUnitario <= {teto}");
        doScript.Should().Equal($"([ValorUnitario]<=({teto}))");
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
