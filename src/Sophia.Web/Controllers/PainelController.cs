using Microsoft.AspNetCore.Mvc;
using Sophia.Application.Painel;

namespace Sophia.Web.Controllers;

public sealed class PainelController(IPainelServico painel) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelamento) =>
        View(await painel.ObterAsync(cancelamento));
}
