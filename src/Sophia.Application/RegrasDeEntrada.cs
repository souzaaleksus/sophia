using Sophia.Domain;
using Sophia.Domain.Recursos;

namespace Sophia.Application;

public static class RegrasDeEntrada
{
    public const decimal MaiorValorAceito = RegrasComuns.MaiorValorAceito;

    public static string CpfObrigatorio => Mensagens.CpfObrigatorio;

    public static string CpfInvalido => Mensagens.CpfInvalido;

    public static string NomeObrigatorio => Mensagens.NomeObrigatorio;

    public static string DataDeNascimentoInvalida => Mensagens.DataDeNascimentoInvalida;

    public static string EmailInvalido => Mensagens.EmailInvalido;

    public static string TelefoneSemDdd => Mensagens.TelefoneSemDdd;

    public static string ValorUnitarioObrigatorio => Mensagens.ValorUnitarioObrigatorio;

    public static string ValorUnitarioAcimaDoMaximo => Mensagens.ValorUnitarioAcimaDoMaximo;

    public static string ValorUnitarioComMaisDeDuasCasas => Mensagens.ValorUnitarioComMaisDeDuasCasas;

    public static string NomeAcimaDoTamanho(int tamanhoMaximo) => Mensagens.NomeAcimaDoTamanho(tamanhoMaximo);

    public static string QuantidadeAcimaDoMaximo(int quantidadeMaxima) => Mensagens.QuantidadeAcimaDoMaximo(quantidadeMaxima);
}
