using Microsoft.Playwright;

namespace Sophia.E2E.Apoio;

public static class Rede
{
    public static async Task DeixarLentaAsync(IPage pagina, TimeSpan atraso)
    {
        var protocolo = await pagina.Context.NewCDPSessionAsync(pagina);
        await protocolo.SendAsync("Network.enable");
        await protocolo.SendAsync("Network.emulateNetworkConditions", new Dictionary<string, object>
        {
            ["offline"] = false,
            ["latency"] = atraso.TotalMilliseconds,
            ["downloadThroughput"] = -1,
            ["uploadThroughput"] = -1,
        });
    }
}
