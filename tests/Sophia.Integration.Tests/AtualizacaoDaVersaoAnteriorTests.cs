using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed partial class AtualizacaoDaVersaoAnteriorTests(BancoDeTestes banco)
{
    private const string ScriptDaVersaoAnterior = "VersoesAnteriores/create-database-pr8.sql";

    private const string ScriptDeVersaoAntiga = "VersoesAnteriores/create-database-pr7.sql";

    private const string Esquema = """
        SELECT CONCAT(t.name COLLATE DATABASE_DEFAULT, '.', c.name COLLATE DATABASE_DEFAULT, ':', TYPE_NAME(c.user_type_id) COLLATE DATABASE_DEFAULT,
            ':', c.max_length, ':', c.precision, ':', c.scale, ':', CAST(c.is_nullable AS INT), ':', c.collation_name COLLATE DATABASE_DEFAULT)
        FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
        UNION ALL
        SELECT CONCAT(OBJECT_NAME(o.parent_object_id) COLLATE DATABASE_DEFAULT, '.', o.name COLLATE DATABASE_DEFAULT, ':', o.type COLLATE DATABASE_DEFAULT,
            ':', cc.definition COLLATE DATABASE_DEFAULT, ':', CAST(cc.is_not_trusted AS INT))
        FROM sys.objects o LEFT JOIN sys.check_constraints cc ON cc.object_id = o.object_id
        WHERE o.parent_object_id <> 0 AND o.is_ms_shipped = 0
        UNION ALL
        SELECT CONCAT(t.name COLLATE DATABASE_DEFAULT, '.', i.name COLLATE DATABASE_DEFAULT, ':', CAST(i.is_unique AS INT), ':', i.filter_definition COLLATE DATABASE_DEFAULT)
        FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
        WHERE i.name IS NOT NULL
        ORDER BY 1
        """;

    private const string ObjetosComData = """
        SELECT CONCAT(SCHEMA_NAME(schema_id) COLLATE DATABASE_DEFAULT, '.', name COLLATE DATABASE_DEFAULT, ':', type COLLATE DATABASE_DEFAULT, ':', CONVERT(VARCHAR(30), modify_date, 126))
        FROM sys.objects WHERE is_ms_shipped = 0 ORDER BY name
        """;

    private const string Itens = """
        SELECT CONCAT(VendaId, ':', ProdutoId, ':', Quantidade, ':', ValorUnitario) FROM dbo.VendaItens ORDER BY Id
        """;

    private const string ItensComONome = """
        SELECT CONCAT(VendaId, ':', ProdutoId, ':', NomeDoProduto COLLATE DATABASE_DEFAULT, ':', Quantidade, ':', ValorUnitario) FROM dbo.VendaItens ORDER BY Id
        """;

    private const string ColunaDoNome = """
        SELECT CONCAT(N'NomeDoProduto:', COL_LENGTH(N'dbo.VendaItens', N'NomeDoProduto'))
        """;

    private const string Cadastros = """
        SELECT CONCAT(Id, ':', Cpf, ':', Nome COLLATE DATABASE_DEFAULT) FROM dbo.Clientes
        UNION ALL SELECT CONCAT(Id, ':', Nome COLLATE DATABASE_DEFAULT, ':', ValorUnitario) FROM dbo.Produtos
        UNION ALL SELECT CONCAT(Id, ':', Data, ':', ClienteId) FROM dbo.Vendas
        """;

    private const string VendasDaVersaoAnterior = """
        INSERT INTO dbo.Clientes (Cpf, Nome) VALUES ('52998224725', N'Maria Santos');
        INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Caneta azul', 2.50), (N'Café Torrado', 999999.99), (N'Caderno', 18.90);
        INSERT INTO dbo.Vendas (Data, ClienteId) VALUES ('2026-11-13', 1), ('2026-11-14', 1);
        INSERT INTO dbo.VendaItens (VendaId, ProdutoId, Quantidade, ValorUnitario) VALUES (1, 1, 3, 2.50), (1, 2, 1, 999999.99), (2, 1, 10, 2.40);
        SELECT N'pronto';
        """;

    [Fact]
    public async Task Banco_da_versao_anterior_com_vendas_e_atualizado_sem_perder_nada()
    {
        await using var anterior = await CriarPelaVersaoAnteriorAsync("Loja_Anterior");
        await anterior.ConsultarAsync(VendasDaVersaoAnterior);
        var cadastros = await anterior.ConsultarAsync(Cadastros);

        await anterior.ExecutarScriptDoBancoAsync();

        (await anterior.ConsultarAsync(ItensComONome)).Should().Equal(
            "1:1:Caneta azul:3:2.50",
            "1:2:Café Torrado:1:999999.99",
            "2:1:Caneta azul:10:2.40");
        (await anterior.ConsultarAsync(Cadastros)).Should().Equal(cadastros);
        (await anterior.ConsultarAsync(Esquema)).Should().Equal(await banco.ConsultarAsync(Esquema));
    }

    [Fact]
    public async Task Banco_da_versao_anterior_ja_atualizado_nao_muda_na_execucao_seguinte()
    {
        await using var anterior = await CriarPelaVersaoAnteriorAsync("Loja_Anterior");
        await anterior.ConsultarAsync(VendasDaVersaoAnterior);
        await anterior.ExecutarScriptDoBancoAsync();
        var antes = await anterior.ConsultarAsync(ObjetosComData);

        await anterior.ExecutarScriptDoBancoAsync();

        (await anterior.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
        (await anterior.ConsultarAsync(ItensComONome)).Should().HaveCount(3);
    }

    [Fact]
    public async Task Banco_da_versao_anterior_com_valor_acima_do_teto_e_recusado_sem_nada_gravado()
    {
        await using var anterior = await CriarPelaVersaoAnteriorAsync("Loja_Acima");
        await anterior.ConsultarAsync($"""
            {VendasDaVersaoAnterior}
            INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Servidor', 1000000.00), (N'Rack', 2500000.00);
            INSERT INTO dbo.VendaItens (VendaId, ProdutoId, Quantidade, ValorUnitario) VALUES (2, 4, 1, 1000000.00);
            SELECT N'pronto';
            """);
        var antes = await anterior.ConsultarAsync(ObjetosComData);
        var itens = await anterior.ConsultarAsync(Itens);

        var rodar = anterior.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should()
            .Contain("999.999,99").And.Contain("2 em Produtos").And.Contain("1 em VendaItens").And.Contain("nada foi alterado");
        (await anterior.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
        (await anterior.ConsultarAsync(Itens)).Should().Equal(itens);
        (await anterior.ConsultarAsync(ColunaDoNome)).Should().Equal("NomeDoProduto:");
    }

    [Fact]
    public async Task Falha_no_meio_da_atualizacao_nao_deixa_nada_pela_metade()
    {
        await using var anterior = await CriarPelaVersaoAnteriorAsync("Loja_Falha");
        await anterior.ConsultarAsync(VendasDaVersaoAnterior);
        await anterior.ConsultarAsync("EXEC (N'CREATE VIEW dbo.CK_VendaItens_ValorUnitarioMaximo AS SELECT 1 AS Um'); SELECT N'criada';");
        var antes = await anterior.ConsultarAsync(ObjetosComData);
        var itens = await anterior.ConsultarAsync(Itens);

        var rodar = anterior.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should()
            .Contain("CK_VendaItens_ValorUnitarioMaximo").And.NotContain("nada foi alterado");
        (await anterior.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
        (await anterior.ConsultarAsync(Itens)).Should().Equal(itens);
        (await anterior.ConsultarAsync(ColunaDoNome)).Should().Equal("NomeDoProduto:");
    }

    [Fact]
    public async Task Banco_de_versao_mais_antiga_que_a_anterior_continua_recusado_sem_nada_gravado()
    {
        await using var antigo = BancoAvulso.Reservar(BancoDeTestes.Servidor, "Loja_Antiga");
        await antigo.ExecutarScriptDeOutraVersaoAsync(ScriptDeVersaoAntiga);
        await antigo.ConsultarAsync(VendasDaVersaoAnterior);
        var antes = await antigo.ConsultarAsync(ObjetosComData);

        var rodar = antigo.ExecutarScriptDoBancoAsync;

        (await rodar.Should().ThrowAsync<SqlException>()).Which.Message.Should()
            .Contain("versão antiga").And.Contain("CK_VendaItens_QuantidadeMaxima").And.Contain("nada foi alterado");
        (await antigo.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
        (await antigo.ConsultarAsync(ColunaDoNome)).Should().Equal("NomeDoProduto:");
    }

    [Fact]
    public async Task Pelo_sqlcmd_o_banco_da_versao_anterior_e_atualizado()
    {
        await using var anterior = await CriarPelaVersaoAnteriorAsync("Loja_Sqlcmd");
        await anterior.ConsultarAsync(VendasDaVersaoAnterior);

        var (saida, texto) = await Sqlcmd.RodarScriptDoBancoAsync(anterior);

        saida.Should().Be(0, texto);
        (await anterior.ConsultarAsync(ItensComONome)).Should().HaveCount(3).And.Contain("1:2:Café Torrado:1:999999.99");
    }

    [Fact]
    public async Task Pelo_sqlcmd_sem_parar_no_erro_a_recusa_do_teto_e_o_unico_erro_e_nada_e_gravado()
    {
        await using var anterior = await CriarPelaVersaoAnteriorAsync("Loja_Acima");
        await anterior.ConsultarAsync($"""
            {VendasDaVersaoAnterior}
            INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Servidor', 1000000.00);
            SELECT N'pronto';
            """);
        var antes = await anterior.ConsultarAsync(ObjetosComData);

        var (_, texto) = await Sqlcmd.RodarScriptDoBancoAsync(anterior, pararNoErro: false);

        texto.Should().Contain("1 em Produtos").And.NotContain("em VendaItens");
        CabecalhoDeErro().Matches(texto).Should().ContainSingle("a saída do sqlcmd foi: {0}", texto);
        (await anterior.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
    }

    [Fact]
    public async Task Pelo_sqlcmd_sem_parar_no_erro_a_recusa_de_versao_antiga_e_o_unico_erro()
    {
        await using var antigo = BancoAvulso.Reservar(BancoDeTestes.Servidor, "Loja_Antiga");
        await antigo.ExecutarScriptDeOutraVersaoAsync(ScriptDeVersaoAntiga);
        var antes = await antigo.ConsultarAsync(ObjetosComData);

        var (_, texto) = await Sqlcmd.RodarScriptDoBancoAsync(antigo, pararNoErro: false);

        texto.Should().Contain("versão antiga");
        CabecalhoDeErro().Matches(texto).Should().ContainSingle("a saída do sqlcmd foi: {0}", texto);
        (await antigo.ConsultarAsync(ObjetosComData)).Should().Equal(antes);
    }

    private static async Task<BancoAvulso> CriarPelaVersaoAnteriorAsync(string prefixo)
    {
        var anterior = BancoAvulso.Reservar(BancoDeTestes.Servidor, prefixo);
        try
        {
            await anterior.ExecutarScriptDeOutraVersaoAsync(ScriptDaVersaoAnterior);
            return anterior;
        }
        catch
        {
            await anterior.DisposeAsync();
            throw;
        }
    }

    [GeneratedRegex(@"^(?:Msg|Mensagem) \d+,", RegexOptions.Multiline)]
    private static partial Regex CabecalhoDeErro();
}
