using Microsoft.Playwright;

namespace Sophia.E2E.Apoio;

public static class Rede
{
    public static Task DeixarLentaAsync(IPage pagina, TimeSpan atraso) =>
        pagina.RouteAsync("**/*", async rota =>
        {
            await Task.Delay(atraso);
            await rota.FallbackAsync();
        });

    public static Task BuscarForaDoNavegadorAsync(IBrowserContext contexto) =>
        contexto.RouteAsync("**/*", async rota =>
        {
            var resposta = await rota.FetchAsync(new() { MaxRedirects = 0 });
            await rota.FulfillAsync(new() { Response = resposta });
        });
}
