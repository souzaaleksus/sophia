using System.Text.RegularExpressions;

namespace Sophia.Testes.Banco;

internal static partial class Lotes
{
    public static IEnumerable<string> Separar(string script) =>
        SeparadorDeLotes().Split(script).Where(lote => !string.IsNullOrWhiteSpace(lote));

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorDeLotes();
}
