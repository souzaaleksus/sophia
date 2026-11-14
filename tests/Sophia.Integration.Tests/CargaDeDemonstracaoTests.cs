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

    private async Task<int> ContarAsync(string tabela) =>
        int.Parse((await banco.ConsultarAsync($"SELECT CAST(COUNT(*) AS VARCHAR(10)) FROM dbo.{tabela}")).Single(),
            System.Globalization.CultureInfo.InvariantCulture);
}
