using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests.Apoio;

internal static class ComandoDoReadme
{
    public const string NomeDoExemplo = "SophiaAvaliacao";

    public const string ConexaoNoPowerShell = @"-S .\SQLEXPRESS -E";

    public const string ConexaoNoBash = "-S localhost -U sa";

    public static string Raiz { get; } = LocalizarRaiz();

    public static string Linha(Func<string, bool> criterio)
    {
        var linhas = File.ReadAllLines(Path.Combine(Raiz, "README.md"))
            .Select(linha => linha.Trim())
            .Where(linha => linha.Contains("database/create-database.sql", StringComparison.Ordinal)
                && linha.Contains("sqlcmd ", StringComparison.Ordinal))
            .Where(criterio)
            .ToList();

        return linhas.Count == 1
            ? linhas[0]
            : throw new InvalidOperationException($"O README devia ter uma linha de comando desse tipo e tem {linhas.Count}.");
    }

    public static string Conexao(bool powerShell)
    {
        var conexao = new SqlConnectionStringBuilder(BancoDeTestes.Servidor);
        var aspa = powerShell ? "''" : @"'\''";
        string Citar(string valor) => $"'{valor.Replace("'", aspa, StringComparison.Ordinal)}'";
        return conexao.IntegratedSecurity
            ? $"-S {Citar(conexao.DataSource)} -E"
            : $"-S {Citar(conexao.DataSource)} -U {Citar(conexao.UserID)}";
    }

    public static async Task<(int Saida, string Texto)> RodarAsync(string executavel, params string[] argumentos)
    {
        var inicio = new ProcessStartInfo(executavel)
        {
            UseShellExecute = false,
            WorkingDirectory = Raiz,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argumento in argumentos)
        {
            inicio.ArgumentList.Add(argumento);
        }

        var conexao = new SqlConnectionStringBuilder(BancoDeTestes.Servidor);
        inicio.Environment.Remove("SQLCMDPASSWORD");
        if (!conexao.IntegratedSecurity)
        {
            inicio.Environment["SQLCMDPASSWORD"] = conexao.Password;
        }

        using var processo = Process.Start(inicio)!;
        var saida = processo.StandardOutput.ReadToEndAsync();
        var erro = processo.StandardError.ReadToEndAsync();
        await processo.WaitForExitAsync();
        return (processo.ExitCode, await saida + await erro);
    }

    public static string PowerShell() =>
        Procurar(OperatingSystem.IsWindows() ? ["pwsh.exe", "powershell.exe"] : ["pwsh"])
        ?? throw new FileNotFoundException("O PowerShell (pwsh) não foi encontrado no PATH.");

    public static string Bash()
    {
        if (!OperatingSystem.IsWindows())
        {
            return Procurar(["bash"]) ?? "/bin/bash";
        }

        var doGit = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe");
        return File.Exists(doGit) ? doGit : throw new FileNotFoundException("O bash do Git não foi encontrado.", doGit);
    }

    private static string? Procurar(string[] nomes) =>
        nomes
            .SelectMany(nome => (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(pasta => Path.Combine(pasta, nome)))
            .FirstOrDefault(File.Exists);

    private static string LocalizarRaiz()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Sophia.sln")))
        {
            pasta = pasta.Parent;
        }

        return pasta?.FullName ?? throw new DirectoryNotFoundException("A pasta do Sophia.sln não foi encontrada.");
    }
}
