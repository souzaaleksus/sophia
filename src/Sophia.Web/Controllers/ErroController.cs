using Microsoft.AspNetCore.Mvc;

namespace Sophia.Web.Controllers;

[Route("Erro")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErroController : Controller
{
    [Route("")]
    [Route("{codigo:int}")]
    public IActionResult Index(int? codigo) => View(codigo ?? StatusCodes.Status500InternalServerError);
}
