using Microsoft.Data.SqlClient;

namespace Sophia.Testes.Banco;

public sealed class BancoAvulso : IAsyncDisposable
{
    public const string AgrupamentoDoSophia = "Latin1_General_CI_AS";
    public const string ScriptDoBanco = "create-database.sql";

    private BancoAvulso(string servidor, string nome)
    {
        Servidor = servidor;
        Nome = nome;
        TextoDeConexao = new SqlConnectionStringBuilder(servidor) { InitialCatalog = nome, Pooling = false }.ConnectionString;
    }

    public string Servidor { get; }

    public string Nome { get; }

    public string TextoDeConexao { get; }

    public static BancoAvulso Reservar(string servidor, string prefixo = "Sophia_Testes") =>
        new(servidor, $"{prefixo}_{Guid.NewGuid():N}");

    public static async Task<BancoAvulso> CriarVazioAsync(
        string servidor,
        string prefixo = "Sophia_Testes",
        string agrupamento = AgrupamentoDoSophia)
    {
        var banco = Reservar(servidor, prefixo);
        await ExecutarNoServidorAsync(servidor, $"CREATE DATABASE [{banco.Nome}] COLLATE {agrupamento};");
        return banco;
    }

    public static async Task<BancoAvulso> CriarPeloScriptAsync(string servidor, string prefixo = "Sophia_Testes")
    {
        var banco = Reservar(servidor, prefixo);
        await banco.ExecutarScriptDoBancoAsync();
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

    public static string CaminhoDoArquivo(string arquivo) => Path.Combine(AppContext.BaseDirectory, arquivo);

    public static string ScriptDoBancoPara(string nome)
    {
        var linhas = File.ReadAllLines(CaminhoDoArquivo(ScriptDoBanco));
        var fimDoTopo = Array.FindIndex(linhas, linha => linha.StartsWith("USE [", StringComparison.Ordinal));
        for (var linha = 0; linha <= fimDoTopo; linha++)
        {
            linhas[linha] = linhas[linha]
                .Replace("N'Sophia'", $"N'{nome}'", StringComparison.Ordinal)
                .Replace("[Sophia]", $"[{nome}]", StringComparison.Ordinal);
        }

        var script = string.Join('\n', linhas);
        if (fimDoTopo < 0
            || script.Contains("[Sophia]", StringComparison.OrdinalIgnoreCase)
            || script.Contains("N'Sophia'", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("O script do banco ainda cita o banco Sophia depois da troca do nome.");
        }

        return script;
    }

    public Task ExecutarScriptDoBancoAsync() =>
        ExecutarLotesAsync(
            new SqlConnectionStringBuilder(Servidor) { InitialCatalog = "master" }.ConnectionString,
            ScriptDoBancoPara(Nome));

    public async Task ExecutarScriptDeOutraVersaoAsync(string arquivo)
    {
        var script = (await File.ReadAllTextAsync(CaminhoDoArquivo(arquivo)))
            .Replace("[Sophia]", $"[{Nome}]", StringComparison.Ordinal)
            .Replace("N'Sophia'", $"N'{Nome}'", StringComparison.Ordinal);
        await ExecutarLotesAsync(new SqlConnectionStringBuilder(Servidor) { InitialCatalog = "master" }.ConnectionString, script);
    }

    public static async Task ExecutarArquivoAsync(string textoDeConexao, string arquivo) =>
        await ExecutarLotesAsync(textoDeConexao, await File.ReadAllTextAsync(CaminhoDoArquivo(arquivo)));

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

    private static async Task ExecutarLotesAsync(string textoDeConexao, string script)
    {
        await using var conexao = new SqlConnection(new SqlConnectionStringBuilder(textoDeConexao) { Pooling = false }.ConnectionString);
        await conexao.OpenAsync();
        foreach (var lote in Lotes.Separar(script))
        {
            await using var comando = new SqlCommand(lote, conexao) { CommandTimeout = 0 };
            await comando.ExecuteNonQueryAsync();
        }
    }

    private static async Task ExecutarNoServidorAsync(string servidor, string sql)
    {
        await using var conexao = new SqlConnection(new SqlConnectionStringBuilder(servidor) { Pooling = false }.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao) { CommandTimeout = 0 };
        await comando.ExecuteNonQueryAsync();
    }
}
