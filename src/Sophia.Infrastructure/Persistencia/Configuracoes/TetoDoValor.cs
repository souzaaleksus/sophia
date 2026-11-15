using System.Globalization;
using Sophia.Domain;

namespace Sophia.Infrastructure.Persistencia.Configuracoes;

internal static class TetoDoValor
{
    public static string Sql { get; } =
        string.Create(CultureInfo.InvariantCulture, $"ValorUnitario <= {RegrasComuns.MaiorValorAceito}");
}
