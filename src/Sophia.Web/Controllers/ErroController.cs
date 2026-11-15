using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Sophia.Web.Controllers;

[Route("Erro")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErroController : Controller
{
    [Route("")]
    [Route("{codigo:int}")]
    [IgnoreAntiforgeryToken]
    [SuppressMessage("Security", "S4502", Justification = "A página de erro só exibe; o envio recusado é reexecutado aqui e precisa mostrar a página.")]
    public IActionResult Index(int? codigo) =>
        View(codigo ?? StatusCodes.Status500InternalServerError);
}
