using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E.Apoio;

public static class Exclusao
{
    public static ILocator Link(IPage pagina, string nome) =>
        pagina.GetByRole(AriaRole.Link, new() { Name = $"Excluir {nome}" });

    public static async Task<ILocator> AbrirDialogoAsync(IPage pagina, string nome)
    {
        var link = Link(pagina, nome);
        await Expect(link).ToHaveAttributeAsync("aria-haspopup", "dialog");
        await link.ClickAsync();
        var dialogo = pagina.GetByRole(AriaRole.Dialog);
        await Expect(dialogo).ToBeVisibleAsync();
        return dialogo;
    }
}
