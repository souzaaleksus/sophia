using System.Text;
using Microsoft.Data.SqlClient;
using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ScriptDoBancoTests(BancoDeTestes banco)
{
    private const string Tabelas = "SELECT SCHEMA_NAME(schema_id) + '.' + name FROM sys.tables ORDER BY name, schema_id";

    private const string TabelasDoSophia = "SELECT name FROM sys.tables ORDER BY name";

    private const string ObjetosComData = """
        SELECT CONCAT(SCHEMA_NAME(schema_id) COLLATE DATABASE_DEFAULT, '.', name COLLATE DATABASE_DEFAULT, ':', type COLLATE DATABASE_DEFAULT, ':', CONVERT(VARCHAR(30), modify_date, 126))
        FROM sys.objects WHERE is_ms_shipped = 0 ORDER BY name
        """;

    private const string AgrupamentoDoBanco = "SELECT CONVERT(NVARCHAR(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation'))";

    private const string OutroSistema = "já tem tabelas de outro sistema";

    private const string VersaoAntiga = "versão antiga";

    private const string UmaVendaSemItens = """
        INSERT INTO dbo.Clientes (Cpf, Nome) VALUES ('52998224725', N'Maria Santos');
        INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Caneta azul', 2.50), (N'Caderno', 18.90);
        INSERT INTO dbo.Vendas (Data, ClienteId) VALUES ('2026-11-13', 1);
        """;

    [Fact]
    public async Task Cria_as_quatro_tabelas()
    {
        var tabelas = await banco.ConsultarAsync(TabelasDoSophia);

        tabelas.Should().Equal("Clientes", "Produtos", "VendaItens", "Vendas");
    }

    [Fact]
    public async Task Cria_o_banco_com_o_agrupamento_do_sophia()
    {
        (await banco.ConsultarAsync(AgrupamentoDoBanco)).Should().Equal(BancoAvulso.AgrupamentoDoSophia);
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
        (await banco.ConsultarAsync("""
            SELECT CONCAT(TYPE_NAME(system_type_id), ':', CAST(is_nullable AS INT))
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Vendas') AND name = N'ChaveDeRegistro'
            """)).Should().Equal("uniqueidentifier:1");
        (await banco.ConsultarAsync("""
            SELECT CONCAT(CAST(is_unique AS INT), ':', CAST(has_filter AS INT), ':', filter_definition)
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Vendas') AND name = N'UQ_Vendas_ChaveDeRegistro'
            """)).Should().Equal("1:1:([ChaveDeRegistro] IS NOT NULL)");
    }

    [Fact]
    public async Task Rodar_o_script_de_novo_num_banco_ja_criado_nao_muda_nada()
    {
        await banco.LimparAsync();
        await banco.ExecutarAsync(UmaVendaSemItens);
        var antes = await banco.ConsultarAsync(ObjetosComData);

        await banco.ExecutarScriptAsync();
        await banco.ExecutarScriptAsync();

        (await banco.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
        (await banco.ConsultarAsync("SELECT Nome FROM dbo.Produtos ORDER BY Id")).Should().Equal("Caneta azul", "Caderno");
        await banco.LimparAsync();
    }

    [Fact]
    public async Task Banco_com_tabelas_de_outro_sistema_e_recusado_sem_nada_gravado()
    {
        await using var outro = await BancoAvulso.CriarVazioAsync(BancoDeTestes.Servidor, "Loja_Alheia");
        await outro.ConsultarAsync("CREATE TABLE dbo.Pedidos (Id INT); SELECT N'criada';");

        var rodar = outro.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain(OutroSistema).And.Contain(outro.Nome);
        (await outro.ConsultarAsync(Tabelas)).Should().Equal("dbo.Pedidos");
    }

    [Fact]
    public async Task Banco_do_sophia_com_uma_tabela_alheia_e_recusado_sem_nada_gravado()
    {
        await using var outro = await BancoAvulso.CriarPeloScriptAsync(BancoDeTestes.Servidor, "Loja_Misturada");
        await outro.ConsultarAsync("EXEC (N'CREATE SCHEMA estoque'); CREATE TABLE estoque.Clientes (Id INT); SELECT N'criada';");
        var antes = await outro.ConsultarAsync(ObjetosComData);

        var rodar = outro.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain(OutroSistema);
        (await outro.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
    }

    [Fact]
    public async Task Banco_existente_com_outro_agrupamento_e_recusado_sem_nada_gravado()
    {
        await using var outro = await BancoAvulso.CriarVazioAsync(
            BancoDeTestes.Servidor, "Loja_Agrupamento", "SQL_Latin1_General_CP1_CI_AS");

        var rodar = outro.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should()
            .Contain("SQL_Latin1_General_CP1_CI_AS").And.Contain(BancoAvulso.AgrupamentoDoSophia);
        (await outro.ConsultarAsync(Tabelas)).Should().BeEmpty();
        (await outro.ConsultarAsync(AgrupamentoDoBanco)).Should().Equal("SQL_Latin1_General_CP1_CI_AS");
    }

    [Fact]
    public async Task Banco_criado_por_versao_antiga_do_script_e_recusado_sem_nada_gravado()
    {
        await using var antigo = await BancoAvulso.CriarPeloScriptAsync(BancoDeTestes.Servidor, "Loja_Antiga");
        await antigo.ConsultarAsync($"""
            ALTER TABLE dbo.VendaItens DROP CONSTRAINT CK_VendaItens_QuantidadeMaxima;
            {UmaVendaSemItens}
            SELECT N'pronto';
            """);
        var antes = await antigo.ConsultarAsync(ObjetosComData);

        var rodar = antigo.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should()
            .Contain(VersaoAntiga).And.Contain("CK_VendaItens_QuantidadeMaxima");
        (await antigo.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
        (await antigo.ConsultarAsync("SELECT Nome FROM dbo.Clientes")).Should().Equal("Maria Santos");
    }

    [Fact]
    public async Task Banco_sem_so_o_nome_do_produto_gravado_na_venda_e_recusado_sem_nada_gravado()
    {
        await using var antigo = await BancoAvulso.CriarPeloScriptAsync(BancoDeTestes.Servidor, "Loja_Antiga");
        await antigo.ConsultarAsync("ALTER TABLE dbo.VendaItens DROP COLUMN NomeDoProduto; SELECT N'pronto';");
        var antes = await antigo.ConsultarAsync(ObjetosComData);

        var rodar = antigo.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should()
            .Contain(VersaoAntiga).And.Contain("VendaItens.NomeDoProduto");
        (await antigo.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
    }

    [Fact]
    public async Task O_nome_do_produto_gravado_na_venda_e_obrigatorio_e_ignora_acentos()
    {
        (await banco.ConsultarAsync("""
            SELECT CONCAT(TYPE_NAME(system_type_id), ':', max_length / 2, ':', CAST(is_nullable AS INT), ':', collation_name)
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.VendaItens') AND name = N'NomeDoProduto'
            """)).Should().Equal("nvarchar:150:0:Latin1_General_CI_AI");
    }

    [Fact]
    public async Task Banco_com_parte_das_tabelas_do_sophia_e_recusado_sem_nada_gravado()
    {
        await using var antigo = await BancoAvulso.CriarVazioAsync(BancoDeTestes.Servidor, "Loja_Parcial");
        await antigo.ConsultarAsync("CREATE TABLE dbo.Clientes (Id INT); SELECT N'criada';");

        var rodar = antigo.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain(VersaoAntiga).And.Contain("Produtos.Nome");
        (await antigo.ConsultarAsync(Tabelas)).Should().Equal("dbo.Clientes");
    }

    [Fact]
    public async Task Falha_no_meio_da_criacao_nao_deixa_nenhuma_tabela()
    {
        await using var vazio = await BancoAvulso.CriarVazioAsync(BancoDeTestes.Servidor, "Loja_Falha");
        await vazio.ConsultarAsync("EXEC (N'CREATE VIEW dbo.UQ_VendaItens_Venda_Produto AS SELECT 1 AS Um'); SELECT N'criada';");

        var rodar = vazio.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain("UQ_VendaItens_Venda_Produto");
        (await vazio.ConsultarAsync(Tabelas)).Should().BeEmpty();
    }

    [Fact]
    public async Task Pelo_sqlcmd_cria_o_banco_novo()
    {
        await using var novo = BancoAvulso.Reservar(BancoDeTestes.Servidor, "Loja_Sqlcmd");

        var (saida, texto) = await Sqlcmd.RodarScriptDoBancoAsync(novo);

        saida.Should().Be(0, texto);
        (await novo.ConsultarAsync(TabelasDoSophia)).Should().Equal("Clientes", "Produtos", "VendaItens", "Vendas");
    }

    [Fact]
    public async Task Pelo_sqlcmd_a_recusa_sai_com_os_acentos_integros()
    {
        await using var outro = await BancoAvulso.CriarVazioAsync(BancoDeTestes.Servidor, "Loja_Acentos");
        await outro.ConsultarAsync("CREATE TABLE dbo.Pedidos (Id INT); SELECT N'criada';");

        var (saida, texto) = await Sqlcmd.RodarScriptDoBancoAsync(outro);

        saida.Should().NotBe(0);
        texto.Should().Contain(OutroSistema).And.Contain("nada foi alterado");
        (await outro.ConsultarAsync(Tabelas)).Should().Equal("dbo.Pedidos");
    }

    [Fact]
    public async Task Pelo_sqlcmd_sem_parar_no_erro_a_recusa_tambem_nao_grava_nada()
    {
        await using var outro = await BancoAvulso.CriarVazioAsync(BancoDeTestes.Servidor, "Loja_SemParar");
        await outro.ConsultarAsync("CREATE TABLE dbo.Pedidos (Id INT); SELECT N'criada';");

        var (_, texto) = await Sqlcmd.RodarScriptDoBancoAsync(outro, pararNoErro: false);

        texto.Should().Contain(OutroSistema);
        (await outro.ConsultarAsync(Tabelas)).Should().Equal("dbo.Pedidos");
    }

    [Fact]
    public async Task Pelo_sqlcmd_conectado_a_outro_banco_e_recusado_sem_criar_nem_gravar_nada()
    {
        await using var conectado = await BancoAvulso.CriarVazioAsync(BancoDeTestes.Servidor, "Loja_Conectada");
        await using var alvo = BancoAvulso.Reservar(BancoDeTestes.Servidor, "Loja_Alvo");

        var (saida, texto) = await Sqlcmd.RodarScriptDoBancoAsync(alvo, banco: conectado.Nome, pararNoErro: false);

        saida.Should().Be(0, "sem -b o sqlcmd não devolve erro, e por isso o lote de criação também tem de ficar parado");
        texto.Should().Contain("conectado ao master").And.Contain("comando pronto do README");
        (await conectado.ConsultarAsync(Tabelas)).Should().BeEmpty();
        (await BancoAvulso.ExisteAsync(BancoDeTestes.Servidor, alvo.Nome)).Should().BeFalse();
    }

    [Fact]
    public void O_arquivo_do_script_comeca_com_a_marca_de_utf8()
    {
        var inicio = File.ReadAllBytes(BancoAvulso.CaminhoDoArquivo(BancoAvulso.ScriptDoBanco)).Take(3);

        inicio.Should().Equal(Encoding.UTF8.Preamble.ToArray());
    }

    [Fact]
    public void O_nome_do_banco_so_aparece_nas_linhas_do_topo()
    {
        var linhas = File.ReadAllLines(BancoAvulso.CaminhoDoArquivo(BancoAvulso.ScriptDoBanco));
        var fimDoTopo = Array.FindIndex(linhas, linha => linha.StartsWith("USE [Sophia];", StringComparison.Ordinal));

        fimDoTopo.Should().BePositive();
        linhas.Take(fimDoTopo).Should().Contain(linha => linha.Contains("CREATE DATABASE [Sophia]", StringComparison.Ordinal));
        linhas.Skip(fimDoTopo + 1).Should().NotContain(linha => linha.Contains("Sophia", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task O_banco_barra_quantidade_acima_do_teto_da_faixa_do_parametro()
    {
        await banco.LimparAsync();
        await banco.ExecutarAsync(UmaVendaSemItens);

        await banco.ExecutarAsync("INSERT INTO dbo.VendaItens (VendaId, ProdutoId, NomeDoProduto, Quantidade, ValorUnitario) VALUES (1, 1, N'Caneta azul', 999999, 2.50);");
        var acimaDoTeto = () => banco.ExecutarAsync(
            "INSERT INTO dbo.VendaItens (VendaId, ProdutoId, NomeDoProduto, Quantidade, ValorUnitario) VALUES (1, 2, N'Caderno', 1000000, 2.50);");

        (await acimaDoTeto.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain("CK_VendaItens_QuantidadeMaxima");
        await banco.LimparAsync();
    }

    [Fact]
    public async Task O_banco_barra_valor_unitario_acima_do_maior_valor_aceito()
    {
        await banco.LimparAsync();
        await banco.ExecutarAsync($"""
            {UmaVendaSemItens}
            INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Notebook', 999999.99);
            INSERT INTO dbo.VendaItens (VendaId, ProdutoId, NomeDoProduto, Quantidade, ValorUnitario) VALUES (1, 1, N'Caneta azul', 1, 999999.99);
            """);

        var produtoAcima = () => banco.ExecutarAsync("INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Servidor', 1000000.00);");
        var itemAcima = () => banco.ExecutarAsync(
            "INSERT INTO dbo.VendaItens (VendaId, ProdutoId, NomeDoProduto, Quantidade, ValorUnitario) VALUES (1, 2, N'Caderno', 1, 1000000.00);");

        (await produtoAcima.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain("CK_Produtos_ValorUnitarioMaximo");
        (await itemAcima.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain("CK_VendaItens_ValorUnitarioMaximo");
        await banco.LimparAsync();
    }

    [Fact]
    public async Task O_indice_do_nome_do_produto_recusa_nome_que_so_difere_no_acento()
    {
        await banco.LimparAsync();
        await banco.ExecutarAsync("INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Café Torrado', 32);");

        var semAcento = () => banco.ExecutarAsync("INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Cafe Torrado', 30);");

        (await semAcento.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain("UQ_Produtos_Nome");
        await banco.LimparAsync();
    }

    [Fact]
    public async Task O_nome_do_cliente_nao_diferencia_acentos_e_continua_sem_ser_unico()
    {
        await banco.LimparAsync();
        await banco.ExecutarAsync("""
            INSERT INTO dbo.Clientes (Cpf, Nome) VALUES ('52998224725', N'José Antônio'), ('11144477735', N'Jose Antonio');
            """);

        (await banco.ConsultarAsync("SELECT CAST(COUNT(*) AS VARCHAR(10)) FROM dbo.Clientes WHERE Nome = N'jose antonio'"))
            .Should().Equal("2");
        await banco.LimparAsync();
    }
}
