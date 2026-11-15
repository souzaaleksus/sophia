using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests.Apoio;

internal static class Sqlcmd
{
    private static readonly string[] PastasConhecidas = ["/opt/mssql-tools18/bin", "/opt/mssql-tools/bin"];

    public static async Task<(int Saida, string Texto)> RodarAsync(
        string servidor, string arquivo, string? banco = null, bool pararNoErro = true, params string[] outros)
    {
        var conexao = new SqlConnectionStringBuilder(servidor);
        var saida = Path.Combine(Path.GetTempPath(), $"sqlcmd_{Guid.NewGuid():N}.txt");
        var inicio = new ProcessStartInfo(Executavel()) { UseShellExecute = false };
        foreach (var argumento in Argumentos(conexao, arquivo, saida, banco, pararNoErro).Concat(outros))
        {
            inicio.ArgumentList.Add(argumento);
        }

        try
        {
            using var processo = Process.Start(inicio)!;
            await processo.WaitForExitAsync();
            var texto = Encoding.Unicode.GetString(await File.ReadAllBytesAsync(saida)).TrimStart('﻿');
            return (processo.ExitCode, texto);
        }
        finally
        {
            File.Delete(saida);
        }
    }

    public static async Task<(int Saida, string Texto)> RodarScriptDoBancoAsync(
        BancoAvulso alvo, string? banco = null, bool pararNoErro = true)
    {
        var original = BancoAvulso.CaminhoDoArquivo(BancoAvulso.ScriptDoBanco);
        var comMarca = (await File.ReadAllBytesAsync(original)).Take(3).SequenceEqual(Encoding.UTF8.Preamble.ToArray());
        var copia = Path.Combine(Path.GetTempPath(), $"script_{Guid.NewGuid():N}.sql");
        await File.WriteAllTextAsync(copia, BancoAvulso.ScriptDoBancoPara(alvo.Nome), new UTF8Encoding(comMarca));
        try
        {
            return await RodarAsync(BancoDeTestes.Servidor, copia, banco, pararNoErro);
        }
        finally
        {
            File.Delete(copia);
        }
    }

    private static IEnumerable<string> Argumentos(
        SqlConnectionStringBuilder conexao, string arquivo, string saida, string? banco, bool pararNoErro)
    {
        yield return "-S";
        yield return conexao.DataSource;
        if (conexao.IntegratedSecurity)
        {
            yield return "-E";
        }
        else
        {
            yield return "-U";
            yield return conexao.UserID;
            yield return "-P";
            yield return conexao.Password;
        }

        if (banco is not null)
        {
            yield return "-d";
            yield return banco;
        }

        if (pararNoErro)
        {
            yield return "-b";
        }

        foreach (var argumento in new[] { "-C", "-u", "-i", arquivo, "-o", saida })
        {
            yield return argumento;
        }
    }

    private static string Executavel()
    {
        var nome = OperatingSystem.IsWindows() ? "sqlcmd.exe" : "sqlcmd";
        var pastas = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Concat(PastasConhecidas);

        return pastas.Select(pasta => Path.Combine(pasta, nome)).FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("O sqlcmd não foi encontrado no PATH nem em /opt/mssql-tools18/bin.", nome);
    }
}
