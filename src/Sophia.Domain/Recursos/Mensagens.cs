using System.Globalization;
using System.Resources;

namespace Sophia.Domain.Recursos;

internal static class Mensagens
{
    private static readonly CultureInfo PortuguesDoBrasil = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly ResourceManager Textos = new(typeof(Mensagens));

    public static string CpfObrigatorio => Ler(nameof(CpfObrigatorio));

    public static string CpfInvalido => Ler(nameof(CpfInvalido));

    public static string NomeObrigatorio => Ler(nameof(NomeObrigatorio));

    public static string DataDeNascimentoNoFuturo => Ler(nameof(DataDeNascimentoNoFuturo));

    public static string DataDeNascimentoInvalida => Ler(nameof(DataDeNascimentoInvalida));

    public static string EmailInvalido => Ler(nameof(EmailInvalido));

    public static string TelefoneSemDdd => Ler(nameof(TelefoneSemDdd));

    public static string ValorUnitarioObrigatorio => Ler(nameof(ValorUnitarioObrigatorio));

    public static string ValorUnitarioNaoPositivo => Ler(nameof(ValorUnitarioNaoPositivo));

    public static string ValorUnitarioAcimaDoMaximo =>
        string.Format(PortuguesDoBrasil, Ler(nameof(ValorUnitarioAcimaDoMaximo)), RegrasComuns.MaiorValorAceito.ToString("N2", PortuguesDoBrasil));

    public static string ValorUnitarioComMaisDeDuasCasas => Ler(nameof(ValorUnitarioComMaisDeDuasCasas));

    public static string DataDaVendaObrigatoria => Ler(nameof(DataDaVendaObrigatoria));

    public static string DataDaVendaNoFuturo => Ler(nameof(DataDaVendaNoFuturo));

    public static string ClienteNaoSelecionado => Ler(nameof(ClienteNaoSelecionado));

    public static string VendaSemItens => Ler(nameof(VendaSemItens));

    public static string ProdutoNaoSelecionado => Ler(nameof(ProdutoNaoSelecionado));

    public static string ProdutoRepetidoNaVenda => Ler(nameof(ProdutoRepetidoNaVenda));

    public static string ProdutoNaoEncontrado => Ler(nameof(ProdutoNaoEncontrado));

    public static string QuantidadeObrigatoria => Ler(nameof(QuantidadeObrigatoria));

    public static string QuantidadeNaoPositiva => Ler(nameof(QuantidadeNaoPositiva));

    public static string FalhaSemErros => Ler(nameof(FalhaSemErros));

    public static string ValorDeResultadoComErros => Ler(nameof(ValorDeResultadoComErros));

    public static string NomeAcimaDoTamanho(int tamanhoMaximo) =>
        string.Format(PortuguesDoBrasil, Ler(nameof(NomeAcimaDoTamanho)), tamanhoMaximo);

    public static string QuantidadeAcimaDoMaximo(int quantidadeMaxima) =>
        string.Format(PortuguesDoBrasil, Ler(nameof(QuantidadeAcimaDoMaximo)), quantidadeMaxima.ToString("N0", PortuguesDoBrasil));

    private static string Ler(string chave) =>
        Textos.GetString(chave, PortuguesDoBrasil) ?? throw new MissingManifestResourceException(chave);
}
