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

    public static Task BuscarForaDoNavegadorAsync(IBrowserContext contexto, string endereco) =>
        contexto.RouteAsync("**/*", async rota =>
        {
            if (!rota.Request.Url.StartsWith(endereco + "/", StringComparison.OrdinalIgnoreCase))
            {
                await rota.AbortAsync();
                return;
            }

            var cabecalhos = (await rota.Request.AllHeadersAsync())
                .Where(cabecalho => !string.Equals(cabecalho.Key, "cookie", StringComparison.OrdinalIgnoreCase))
                .ToDictionary();
            var resposta = await rota.FetchAsync(new() { Headers = cabecalhos, MaxRedirects = 0 });
            await rota.FulfillAsync(new() { Response = resposta });
        });
}
