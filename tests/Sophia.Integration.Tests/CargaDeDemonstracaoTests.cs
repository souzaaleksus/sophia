using Microsoft.Data.SqlClient;
using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class CargaDeDemonstracaoTests(BancoDeTestes banco) : IAsyncLifetime
{
    private const string Carga = "carga-de-demonstracao.sql";

    public Task InitializeAsync() => banco.LimparAsync();

    public Task DisposeAsync() => banco.LimparAsync();

    [Fact]
    public async Task Enche_o_banco_vazio_pelo_sqlcmd_sem_opcoes_extras()
    {
        await banco.ExecutarComoOSqlcmdAsync(Carga);

        (await ContarAsync("Produtos")).Should().Be(97);
        (await ContarAsync("Clientes")).Should().Be(320);
        (await ContarAsync("Vendas")).Should().BeGreaterThan(1000);
        (await ContarAsync("VendaItens")).Should().BeGreaterThan(await ContarAsync("Vendas"));
        (await banco.ConsultarAsync("SELECT CAST(MAX(Quantidade) AS VARCHAR(10)) FROM dbo.VendaItens"))
            .Select(maior => int.Parse(maior, System.Globalization.CultureInfo.InvariantCulture))
            .Should().ContainSingle().Which.Should().BeLessThanOrEqualTo(new Sophia.Application.ParametrosDoSophia().QuantidadeMaximaPorItem);
    }

    [Fact]
    public async Task Em_banco_com_dados_nao_grava_nada_e_nao_falha()
    {
        await banco.ExecutarAsync("INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Caderno 96 folhas', 18.90);");

        var carregar = () => banco.ExecutarComoOSqlcmdAsync(Carga);

        await carregar.Should().NotThrowAsync();
        (await ContarAsync("Produtos")).Should().Be(1);
        (await ContarAsync("Clientes")).Should().Be(0);
        (await ContarAsync("Vendas")).Should().Be(0);
    }

    [Fact]
    public async Task Fora_de_um_banco_criado_pelo_script_recusa_sem_gravar_nada()
    {
        await using var vazio = await BancoAvulso.CriarVazioAsync(BancoDeTestes.Servidor, "Loja_SemTabelas");

        var carregar = () => BancoAvulso.ExecutarScriptAsync(vazio.TextoDeConexao, Carga);

        (await carregar.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(50001);
        (await vazio.ConsultarAsync("SELECT name FROM sys.tables")).Should().BeEmpty();
    }

    private async Task<int> ContarAsync(string tabela) =>
        int.Parse((await banco.ConsultarAsync($"SELECT CAST(COUNT(*) AS VARCHAR(10)) FROM dbo.{tabela}")).Single(),
            System.Globalization.CultureInfo.InvariantCulture);
}
