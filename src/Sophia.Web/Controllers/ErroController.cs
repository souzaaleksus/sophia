using Microsoft.AspNetCore.Mvc;

namespace Sophia.Web.Controllers;

[Route("Erro")]
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErroController : Controller
{
    public const string Clientes = "Clientes";
    public const string Produtos = "Produtos";
    public const string Vendas = "Vendas";
    public const string Painel = "Painel";

    private static readonly string[] Areas = [Clientes, Produtos, Vendas];

    [Route("")]
    [Route("{codigo:int}")]
    public IActionResult Index(int? codigo) =>
        View(codigo ?? StatusCodes.Status500InternalServerError);

    public static string AreaDe(string? caminho)
    {
        var primeiroTrecho = (caminho ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return Areas.FirstOrDefault(area => string.Equals(area, primeiroTrecho, StringComparison.OrdinalIgnoreCase)) ?? Painel;
    }
}
