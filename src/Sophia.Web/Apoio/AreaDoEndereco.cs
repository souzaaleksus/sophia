namespace Sophia.Web.Apoio;

public static class AreaDoEndereco
{
    public const string Clientes = "Clientes";
    public const string Produtos = "Produtos";
    public const string Vendas = "Vendas";
    public const string Painel = "Painel";

    private static readonly string[] Areas = [Clientes, Produtos, Vendas];

    public static string De(string? caminho)
    {
        var primeiroTrecho = (caminho ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return Areas.FirstOrDefault(area => string.Equals(area, primeiroTrecho, StringComparison.OrdinalIgnoreCase)) ?? Painel;
    }
}
