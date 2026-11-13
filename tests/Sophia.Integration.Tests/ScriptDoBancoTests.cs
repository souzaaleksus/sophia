using Sophia.Integration.Tests.Apoio;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ScriptDoBancoTests(BancoDeTestes banco)
{
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
            "UQ_VendaItens_Venda_Produto");
    }
}
