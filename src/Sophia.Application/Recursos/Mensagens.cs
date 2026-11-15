using System.Globalization;
using System.Resources;
using Sophia.Application.Abstracoes;

namespace Sophia.Application.Recursos;

internal static class Mensagens
{
    private static readonly CultureInfo PortuguesDoBrasil = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly ResourceManager Textos = new(typeof(Mensagens));

    public static string CpfDuplicado => Ler(nameof(CpfDuplicado));

    public static string ClienteNaoEncontrado => Ler(nameof(ClienteNaoEncontrado));

    public static string ClientePossuiVendas => Ler(nameof(ClientePossuiVendas));

    public static string NomeDeProdutoDuplicado => Ler(nameof(NomeDeProdutoDuplicado));

    public static string ProdutoJaVendido => Ler(nameof(ProdutoJaVendido));

    public static string FormularioJaUsado(int venda) =>
        string.Format(PortuguesDoBrasil, Ler(nameof(FormularioJaUsado)), venda);

    public static string GravacaoRecusada(TipoDeConflito tipo) =>
        string.Format(PortuguesDoBrasil, Ler(nameof(GravacaoRecusada)), tipo);

    private static string Ler(string chave) =>
        Textos.GetString(chave, PortuguesDoBrasil) ?? throw new MissingManifestResourceException(chave);
}
