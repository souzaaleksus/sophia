namespace Sophia.Domain;

internal static class RegrasComuns
{
    private const decimal MaiorValorAceito = 9_999_999_999_999_999.99m;

    public static string? ValidarNome(string? nome, int tamanhoMaximo, List<Erro> erros)
    {
        var aparado = Texto.Aparar(nome);
        if (aparado is null)
        {
            erros.Add(new Erro("Nome", "Informe o nome."));
            return null;
        }

        if (aparado.Length > tamanhoMaximo)
        {
            erros.Add(new Erro("Nome", $"O nome deve ter no máximo {tamanhoMaximo} caracteres."));
            return null;
        }

        return aparado;
    }

    public static decimal? ValidarValorUnitario(decimal? valor, string campo, List<Erro> erros)
    {
        string? problema = valor switch
        {
            null => "Informe o valor unitário.",
            <= 0 => "O valor unitário deve ser maior que zero.",
            > MaiorValorAceito => "O valor unitário está acima do permitido.",
            _ when decimal.Round(valor.Value, 2) != valor.Value => "O valor unitário aceita no máximo duas casas decimais.",
            _ => null,
        };

        if (problema is null)
        {
            return valor;
        }

        erros.Add(new Erro(campo, problema));
        return null;
    }
}
