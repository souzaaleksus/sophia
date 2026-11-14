using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sophia.Infrastructure.Persistencia;
using Xunit;

namespace Sophia.Testes.Banco;

public sealed partial class BancoDeTestes : IAsyncLifetime
{
    private const string ServidorPadrao =
        @"Server=.\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true";

    private static readonly string[] TabelasNaOrdemDasChaves = ["VendaItens", "Vendas", "Produtos", "Clientes"];

    private readonly string _nome = $"Sophia_Testes_{Guid.NewGuid():N}";

    public string TextoDeConexao { get; private set; } = string.Empty;

    public static string Servidor =>
        Environment.GetEnvironmentVariable("SOPHIA_TESTES_SQLSERVER") is { Length: > 0 } servidor
            ? servidor
            : ServidorPadrao;

    public async Task InitializeAsync()
    {
        await ExecutarScriptAsync();
        TextoDeConexao = new SqlConnectionStringBuilder(Servidor) { InitialCatalog = _nome }.ConnectionString;
    }

    public async Task ExecutarScriptAsync()
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "create-database.sql"));
        script = script.Replace("[Sophia]", $"[{_nome}]", StringComparison.Ordinal)
            .Replace("N'Sophia'", $"N'{_nome}'", StringComparison.Ordinal);

        await using var conexao = new SqlConnection(Servidor);
        await conexao.OpenAsync();
        foreach (var lote in SeparadorDeLotes().Split(script).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            await using var comando = new SqlCommand(lote, conexao);
            await comando.ExecuteNonQueryAsync();
        }
    }

    public async Task ExecutarAsync(string sql)
    {
        await using var conexao = new SqlConnection(TextoDeConexao);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await comando.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await using var conexao = new SqlConnection(Servidor);
        await conexao.OpenAsync();
        var apagar = $"ALTER DATABASE [{_nome}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_nome}];";
        await using var comando = new SqlCommand(apagar, conexao);
        await comando.ExecuteNonQueryAsync();
    }

    public SophiaDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<SophiaDbContext>().UseSqlServer(TextoDeConexao).Options);

    public SophiaDbContext NovoContexto(Action<string> registrarSql) =>
        new(new DbContextOptionsBuilder<SophiaDbContext>()
            .UseSqlServer(TextoDeConexao)
            .LogTo(registrarSql, [DbLoggerCategory.Database.Command.Name])
            .Options);

    public Task LimparAsync() =>
        ExecutarAsync(string.Concat(TabelasNaOrdemDasChaves.Select(tabela => $"""
            DELETE FROM dbo.{tabela};
            IF EXISTS (
                SELECT 1 FROM sys.identity_columns
                WHERE object_id = OBJECT_ID(N'dbo.{tabela}') AND last_value IS NOT NULL)
                DBCC CHECKIDENT (N'dbo.{tabela}', RESEED, 0) WITH NO_INFOMSGS;

            """)));

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

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorDeLotes();
}
