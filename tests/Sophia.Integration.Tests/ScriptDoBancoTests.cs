using Microsoft.Data.SqlClient;
using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ScriptDoBancoTests(BancoDeTestes banco)
{
    private const string Tabelas = "SELECT name FROM sys.tables ORDER BY name";

    [Fact]
    public async Task Cria_as_quatro_tabelas()
    {
        var tabelas = await banco.ConsultarAsync("SELECT name FROM sys.tables ORDER BY name");

        tabelas.Should().Equal("Clientes", "Produtos", "VendaItens", "Vendas");
    }

    [Fact]
    public async Task Cria_as_chaves_estrangeiras_com_a_regra_de_exclusao_certa()
    {
        var chaves = await banco.ConsultarAsync(
            "SELECT name + ':' + (delete_referential_action_desc COLLATE DATABASE_DEFAULT) FROM sys.foreign_keys ORDER BY name");

        chaves.Should().Equal(
            "FK_VendaItens_Produtos:NO_ACTION",
            "FK_VendaItens_Vendas:CASCADE",
            "FK_Vendas_Clientes:NO_ACTION");
    }

    [Fact]
    public async Task Cria_os_indices_de_unicidade_e_de_busca()
    {
        var indices = await banco.ConsultarAsync("""
            SELECT i.name FROM sys.indexes i
            JOIN sys.tables t ON t.object_id = i.object_id
            WHERE i.is_primary_key = 0 AND i.name IS NOT NULL
            ORDER BY i.name
            """);

        indices.Should().Equal(
            "IX_Clientes_Nome",
            "IX_VendaItens_ProdutoId",
            "IX_Vendas_ClienteId",
            "IX_Vendas_Data",
            "UQ_Clientes_Cpf",
            "UQ_Produtos_Nome",
            "UQ_VendaItens_Venda_Produto",
            "UQ_Vendas_ChaveDeRegistro");
    }

    [Fact]
    public async Task A_chave_de_registro_e_opcional_e_unica_so_entre_as_vendas_que_a_tem()
    {
        (await ColunaDaChaveAsync()).Should().Equal("uniqueidentifier:1");
        (await IndiceDaChaveAsync()).Should().Equal("1:1:([ChaveDeRegistro] IS NOT NULL)");
    }

    [Fact]
    public async Task Atualiza_um_banco_criado_pela_versao_anterior_do_script()
    {
        await banco.ExecutarAsync("""
            DROP INDEX UQ_Vendas_ChaveDeRegistro ON dbo.Vendas;
            ALTER TABLE dbo.Vendas DROP COLUMN ChaveDeRegistro;
            """);
        (await ColunaDaChaveAsync()).Should().BeEmpty();

        await banco.ExecutarScriptAsync();

        (await ColunaDaChaveAsync()).Should().Equal("uniqueidentifier:1");
        (await IndiceDaChaveAsync()).Should().Equal("1:1:([ChaveDeRegistro] IS NOT NULL)");
    }

    [Fact]
    public async Task Rodar_o_script_de_novo_nao_altera_nem_falha()
    {
        var rodarDuasVezes = async () =>
        {
            await banco.ExecutarScriptAsync();
            await banco.ExecutarScriptAsync();
        };

        await rodarDuasVezes.Should().NotThrowAsync();
        (await banco.ConsultarAsync("SELECT name FROM sys.tables ORDER BY name"))
            .Should().Equal("Clientes", "Produtos", "VendaItens", "Vendas");
        (await IndiceDaChaveAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task No_banco_escolhido_pela_conexao_cria_as_tabelas_nele_sem_tocar_no_banco_sophia()
    {
        var sophiaExistia = await BancoAvulso.ExisteAsync(BancoDeTestes.Servidor, "Sophia");
        await using var outro = await BancoAvulso.CriarVazioAsync(BancoDeTestes.Servidor, "Loja_Auditoria");

        await BancoAvulso.ExecutarScriptAsync(outro.TextoDeConexao);
        await BancoAvulso.ExecutarScriptAsync(outro.TextoDeConexao);

        (await outro.ConsultarAsync(Tabelas)).Should().Equal("Clientes", "Produtos", "VendaItens", "Vendas");
        (await BancoAvulso.ExisteAsync(BancoDeTestes.Servidor, "Sophia")).Should().Be(sophiaExistia);
    }

    [Fact]
    public async Task Banco_escolhido_vazio_com_outro_agrupamento_passa_ao_agrupamento_do_sophia()
    {
        await using var outro = await BancoAvulso.CriarVazioAsync(
            BancoDeTestes.Servidor, "Loja_Agrupamento", "SQL_Latin1_General_CP1_CI_AS");

        await BancoAvulso.ExecutarScriptAsync(outro.TextoDeConexao);

        (await outro.ConsultarAsync("SELECT CONVERT(NVARCHAR(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation'))"))
            .Should().Equal(BancoAvulso.AgrupamentoDoSophia);
        (await outro.ConsultarAsync(Tabelas)).Should().HaveCount(4);
    }

    [Fact]
    public async Task Banco_escolhido_com_tabelas_e_outro_agrupamento_e_recusado_sem_criar_nada()
    {
        await using var outro = await BancoAvulso.CriarVazioAsync(
            BancoDeTestes.Servidor, "Loja_Ocupada", "SQL_Latin1_General_CP1_CI_AS");
        await outro.ConsultarAsync("CREATE TABLE dbo.Alheia (Id INT); SELECT N'criada';");

        var rodar = () => BancoAvulso.ExecutarScriptAsync(outro.TextoDeConexao);

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain("COLLATE Latin1_General_CI_AS");
        (await outro.ConsultarAsync(Tabelas)).Should().Equal("Alheia");
    }

    [Fact]
    public async Task Sem_banco_escolhido_cria_o_banco_sophia_e_rodar_de_novo_nao_falha()
    {
        await using var sophia = await BancoAvulso.SophiaAindaInexistenteAsync();
        var semBancoEscolhido = new SqlConnectionStringBuilder(sophia.Servidor) { InitialCatalog = "master" }.ConnectionString;

        await BancoAvulso.ExecutarScriptAsync(semBancoEscolhido);
        await BancoAvulso.ExecutarScriptAsync(semBancoEscolhido);

        (await sophia.ConsultarAsync(Tabelas)).Should().Equal("Clientes", "Produtos", "VendaItens", "Vendas");
        (await sophia.ConsultarAsync("SELECT CONVERT(NVARCHAR(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation'))"))
            .Should().Equal(BancoAvulso.AgrupamentoDoSophia);
    }

    private Task<List<string>> ColunaDaChaveAsync() =>
        banco.ConsultarAsync("""
            SELECT CONCAT(TYPE_NAME(system_type_id), ':', CAST(is_nullable AS INT))
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Vendas') AND name = N'ChaveDeRegistro'
            """);

    private Task<List<string>> IndiceDaChaveAsync() =>
        banco.ConsultarAsync("""
            SELECT CONCAT(CAST(is_unique AS INT), ':', CAST(has_filter AS INT), ':', filter_definition)
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Vendas') AND name = N'UQ_Vendas_ChaveDeRegistro'
            """);
}
