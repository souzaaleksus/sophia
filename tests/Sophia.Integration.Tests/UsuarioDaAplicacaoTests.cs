using System.Text;
using Microsoft.Data.SqlClient;
using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class UsuarioDaAplicacaoTests(BancoDeTestes banco)
{
    private const string ScriptDoUsuario = "usuario-da-aplicacao.sql";

    [Theory]
    [InlineData("master", true)]
    [InlineData("master", false)]
    [InlineData("tempdb", true)]
    public async Task Conectado_a_banco_de_sistema_e_recusado_sem_criar_login_nem_usuario(string sistema, bool pararNoErro)
    {
        await using var login = new LoginDescartavel(banco);

        var (saida, texto) = await login.RodarAsync(sistema, pararNoErro);

        if (pararNoErro)
        {
            saida.Should().NotBe(0, texto);
        }

        texto.Should().Contain("banco de sistema").And.Contain("nada foi alterado").And.Contain("-d");
        (await login.ExisteAsync()).Should().BeFalse(texto);
        (await login.UsuarioEm(sistema)).Should().BeEmpty(texto);
    }

    [Fact]
    public async Task No_banco_do_sophia_cria_o_usuario_que_le_e_grava()
    {
        await using var login = new LoginDescartavel(banco);

        var (saida, texto) = await login.RodarAsync(login.BancoDoSophia, pararNoErro: true);

        saida.Should().Be(0, texto);
        (await login.ExisteAsync()).Should().BeTrue();
        (await login.UsuarioEm(login.BancoDoSophia)).Should().Equal("db_datareader", "db_datawriter");
    }

    private sealed class LoginDescartavel(BancoDeTestes banco) : IAsyncDisposable
    {
        private readonly string _nome = $"sophia_app_teste_{Guid.NewGuid():N}"[..28];

        public string BancoDoSophia { get; } = new SqlConnectionStringBuilder(banco.TextoDeConexao).InitialCatalog;

        public async Task<(int Saida, string Texto)> RodarAsync(string conectadoA, bool pararNoErro)
        {
            var copia = Path.Combine(Path.GetTempPath(), $"usuario_{Guid.NewGuid():N}.sql");
            var script = (await File.ReadAllTextAsync(BancoAvulso.CaminhoDoArquivo(ScriptDoUsuario)))
                .Replace("sophia_app", _nome, StringComparison.Ordinal);
            await File.WriteAllTextAsync(copia, script, new UTF8Encoding(true));
            try
            {
                return await Sqlcmd.RodarAsync(
                    BancoDeTestes.Servidor, copia, conectadoA, pararNoErro, "-v", $"SenhaDaAplicacao=Teste#{Guid.NewGuid():N}");
            }
            finally
            {
                File.Delete(copia);
            }
        }

        public async Task<bool> ExisteAsync() =>
            (await ConsultarAsync("master", "SELECT CONCAT(N'', SUSER_ID(@nome))")).Single().Length > 0;

        public Task<List<string>> UsuarioEm(string nomeDoBanco) =>
            ConsultarAsync(nomeDoBanco, """
                SELECT papel.name FROM sys.database_role_members membro
                JOIN sys.database_principals papel ON papel.principal_id = membro.role_principal_id
                WHERE membro.member_principal_id = USER_ID(@nome)
                UNION ALL
                SELECT N'usuário sem papel' WHERE USER_ID(@nome) IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.database_role_members WHERE member_principal_id = USER_ID(@nome))
                ORDER BY 1
                """);

        public async ValueTask DisposeAsync()
        {
            foreach (var nomeDoBanco in new[] { "master", "tempdb", BancoDoSophia })
            {
                await ConsultarAsync(nomeDoBanco, $"IF USER_ID(@nome) IS NOT NULL DROP USER [{_nome}]; SELECT N'';");
            }

            await ConsultarAsync("master", $"IF SUSER_ID(@nome) IS NOT NULL DROP LOGIN [{_nome}]; SELECT N'';");
        }

        private async Task<List<string>> ConsultarAsync(string nomeDoBanco, string sql)
        {
            var texto = new SqlConnectionStringBuilder(BancoDeTestes.Servidor) { InitialCatalog = nomeDoBanco, Pooling = false };
            await using var conexao = new SqlConnection(texto.ConnectionString);
            await conexao.OpenAsync();
            await using var comando = new SqlCommand(sql, conexao);
            comando.Parameters.AddWithValue("@nome", _nome);
            await using var leitor = await comando.ExecuteReaderAsync();
            var linhas = new List<string>();
            while (await leitor.ReadAsync())
            {
                linhas.Add(leitor.GetString(0));
            }

            return linhas;
        }
    }
}
