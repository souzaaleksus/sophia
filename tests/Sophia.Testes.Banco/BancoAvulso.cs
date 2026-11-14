using Microsoft.Data.SqlClient;

namespace Sophia.Testes.Banco;

public sealed class BancoAvulso : IAsyncDisposable
{
    public const string AgrupamentoDoSophia = "Latin1_General_CI_AS";

    private BancoAvulso(string servidor, string nome)
    {
        Servidor = servidor;
        Nome = nome;
        TextoDeConexao = new SqlConnectionStringBuilder(servidor) { InitialCatalog = nome, Pooling = false }.ConnectionString;
    }

    public string Servidor { get; }

    public string Nome { get; }

    public string TextoDeConexao { get; }

    public static async Task<BancoAvulso> CriarVazioAsync(
        string servidor,
        string prefixo = "Sophia_Testes",
        string agrupamento = AgrupamentoDoSophia)
    {
        var banco = new BancoAvulso(servidor, $"{prefixo}_{Guid.NewGuid():N}");
        await ExecutarNoServidorAsync(servidor, $"CREATE DATABASE [{banco.Nome}] COLLATE {agrupamento};");
        return banco;
    }

    public static async Task<bool> ExisteAsync(string servidor, string nome)
    {
        await using var conexao = new SqlConnection(servidor);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand("SELECT CAST(CASE WHEN DB_ID(@nome) IS NULL THEN 0 ELSE 1 END AS BIT);", conexao);
        comando.Parameters.AddWithValue("@nome", nome);
        return (bool)(await comando.ExecuteScalarAsync())!;
    }

    public static async Task ExecutarScriptAsync(string textoDeConexao, string arquivo = "create-database.sql")
    {
        await using var conexao = new SqlConnection(new SqlConnectionStringBuilder(textoDeConexao) { Pooling = false }.ConnectionString);
        await conexao.OpenAsync();
        foreach (var lote in Lotes.Separar(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, arquivo))))
        {
            await using var comando = new SqlCommand(lote, conexao) { CommandTimeout = 120 };
            await comando.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<string>> ConsultarAsync(string sql)
    {
        await using var conexao = new SqlConnection(TextoDeConexao);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        var linhas = new List<string>();
        while (await leitor.ReadAsync())
        {
            linhas.Add(leitor.GetString(0));
        }

        return linhas;
    }

    public async ValueTask DisposeAsync()
    {
        if (await ExisteAsync(Servidor, Nome))
        {
            await ExecutarNoServidorAsync(
                Servidor,
                $"ALTER DATABASE [{Nome}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Nome}];");
        }
    }

    private static async Task ExecutarNoServidorAsync(string servidor, string sql)
    {
        await using var conexao = new SqlConnection(new SqlConnectionStringBuilder(servidor) { Pooling = false }.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await comando.ExecuteNonQueryAsync();
    }
}
