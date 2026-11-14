using Sophia.Domain.Recursos;

namespace Sophia.Domain;

internal static class RegrasComuns
{
    private const decimal MaiorValorAceito = 9_999_999_999_999_999.99m;

    public static string? ValidarNome(string? nome, string campo, int tamanhoMaximo, List<Erro> erros)
    {
        var aparado = Texto.Aparar(nome);
        if (aparado is null)
        {
            erros.Add(new Erro(campo, Mensagens.NomeObrigatorio));
            return null;
        }

        if (aparado.Length > tamanhoMaximo)
        {
            erros.Add(new Erro(campo, Mensagens.NomeAcimaDoTamanho(tamanhoMaximo)));
            return null;
        }

        return aparado;
    }

    public static decimal? ValidarValorUnitario(decimal? valor, string campo, int? linha, List<Erro> erros)
    {
        string? problema = valor switch
        {
            null => Mensagens.ValorUnitarioObrigatorio,
            <= 0 => Mensagens.ValorUnitarioNaoPositivo,
            > MaiorValorAceito => Mensagens.ValorUnitarioAcimaDoPermitido,
            _ when decimal.Round(valor.Value, 2) != valor.Value => Mensagens.ValorUnitarioComMaisDeDuasCasas,
            _ => null,
        };

        if (problema is null)
        {
            return valor;
        }

        erros.Add(new Erro(campo, problema, linha));
        return null;
    }
}
