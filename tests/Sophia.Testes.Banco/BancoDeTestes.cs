using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sophia.Infrastructure.Persistencia;
using Xunit;

namespace Sophia.Testes.Banco;

public sealed class BancoDeTestes : IAsyncLifetime
{
    private const string ServidorPadrao =
        @"Server=.\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true";

    private static readonly string[] TabelasNaOrdemDasChaves = ["VendaItens", "Vendas", "Produtos", "Clientes"];

    private BancoAvulso? _banco;

    public string TextoDeConexao { get; private set; } = string.Empty;

    public static string Servidor =>
        Environment.GetEnvironmentVariable("SOPHIA_TESTES_SQLSERVER") is { Length: > 0 } servidor
            ? servidor
            : ServidorPadrao;

    public async Task InitializeAsync()
    {
        _banco = await BancoAvulso.CriarVazioAsync(Servidor);
        TextoDeConexao = new SqlConnectionStringBuilder(Servidor) { InitialCatalog = _banco.Nome }.ConnectionString;
        await ExecutarScriptAsync();
    }

    public Task ExecutarScriptAsync() => BancoAvulso.ExecutarScriptAsync(TextoDeConexao);

    public async Task ExecutarComoOSqlcmdAsync(string arquivo)
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, arquivo));

        await using var conexao = new SqlConnection(new SqlConnectionStringBuilder(TextoDeConexao) { Pooling = false }.ConnectionString);
        await conexao.OpenAsync();
        await using (var padraoDoSqlcmd = new SqlCommand("SET QUOTED_IDENTIFIER OFF;", conexao))
        {
            await padraoDoSqlcmd.ExecuteNonQueryAsync();
        }

        foreach (var lote in Lotes.Separar(script))
        {
            await using var comando = new SqlCommand(lote, conexao) { CommandTimeout = 120 };
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
        if (_banco is not null)
        {
            await _banco.DisposeAsync();
        }
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
}
