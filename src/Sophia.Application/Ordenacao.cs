namespace Sophia.Application;

public sealed record Ordenacao<TColuna>(TColuna Coluna, bool Decrescente)
    where TColuna : struct, Enum
{
    public string Texto => Decrescente ? Ordenacao.Inversao + Ordenacao.NomeDa(Coluna) : Ordenacao.NomeDa(Coluna);
}

public static class Ordenacao
{
    internal const char Inversao = '-';

    public static Ordenacao<TColuna> Interpretar<TColuna>(string? pedida, Ordenacao<TColuna> padrao)
        where TColuna : struct, Enum
    {
        if (string.IsNullOrEmpty(pedida))
        {
            return padrao;
        }

        var decrescente = pedida[0] == Inversao;
        var nome = decrescente ? pedida[1..] : pedida;
        foreach (var coluna in Enum.GetValues<TColuna>())
        {
            if (string.Equals(NomeDa(coluna), nome, StringComparison.Ordinal))
            {
                return new Ordenacao<TColuna>(coluna, decrescente);
            }
        }

        return padrao;
    }

    internal static string NomeDa<TColuna>(TColuna coluna)
        where TColuna : struct, Enum
    {
        var nome = coluna.ToString();
        return char.ToLowerInvariant(nome[0]) + nome[1..];
    }
}
