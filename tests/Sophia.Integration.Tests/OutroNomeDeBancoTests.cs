using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class OutroNomeDeBancoTests
{
    private const string TabelasDoSophia = "SELECT name FROM sys.tables ORDER BY name";

    [Fact]
    public async Task O_comando_do_README_em_PowerShell_cria_o_banco_com_outro_nome_sem_editar_o_script()
    {
        var comando = ComandoDoReadme.Linha(linha => linha.Contains("-replace", StringComparison.Ordinal));
        await using var outro = BancoAvulso.Reservar(BancoDeTestes.Servidor, "Loja_OutroNome");
        var original = await File.ReadAllBytesAsync(Path.Combine(ComandoDoReadme.Raiz, "database", BancoAvulso.ScriptDoBanco));

        var (saida, texto) = await ComandoDoReadme.RodarAsync(
            ComandoDoReadme.PowerShell(), "-NoProfile", "-NonInteractive", "-Command",
            $"{ParaOTeste(comando, outro, ComandoDoReadme.ConexaoNoPowerShell, powerShell: true)}; exit $LASTEXITCODE");

        saida.Should().Be(0, texto);
        (await outro.ConsultarAsync(TabelasDoSophia)).Should().Equal("Clientes", "Produtos", "VendaItens", "Vendas");
        (await File.ReadAllBytesAsync(Path.Combine(ComandoDoReadme.Raiz, "database", BancoAvulso.ScriptDoBanco))).Should().Equal(original);
    }

    [Fact]
    public async Task O_comando_do_README_em_bash_cria_o_banco_com_outro_nome_sem_editar_o_script()
    {
        var comando = ComandoDoReadme.Linha(linha => linha.StartsWith("tail ", StringComparison.Ordinal));
        await using var outro = BancoAvulso.Reservar(BancoDeTestes.Servidor, "Loja_OutroNome");

        var (saida, texto) = await ComandoDoReadme.RodarAsync(
            ComandoDoReadme.Bash(), "-c", ParaOTeste(comando, outro, ComandoDoReadme.ConexaoNoBash, powerShell: false));

        saida.Should().Be(0, texto);
        (await outro.ConsultarAsync(TabelasDoSophia)).Should().Equal("Clientes", "Produtos", "VendaItens", "Vendas");
    }

    private static string ParaOTeste(string comando, BancoAvulso outro, string conexaoDoExemplo, bool powerShell)
    {
        comando.Should().Contain(ComandoDoReadme.NomeDoExemplo).And.Contain(conexaoDoExemplo);
        return comando
            .Replace(ComandoDoReadme.NomeDoExemplo, outro.Nome, StringComparison.Ordinal)
            .Replace(conexaoDoExemplo, ComandoDoReadme.Conexao(powerShell), StringComparison.Ordinal);
    }
}
