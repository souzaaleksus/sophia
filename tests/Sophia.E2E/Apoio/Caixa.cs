using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E.Apoio;

public sealed class Caixa(IPage pagina)
{
    private const int TentativasDeBusca = 3;
    private const float EsperaPelaSugestao = 5000;

    public ILocator ComboboxDeCliente =>
        pagina.GetByRole(AriaRole.Combobox, new() { Name = "Cliente", Exact = true });

    public ILocator Itens => pagina.GetByTestId("item-da-venda");

    public ILocator Total => pagina.GetByTestId("total-da-venda");

    public ILocator Finalizar => pagina.GetByRole(AriaRole.Button, new() { Name = "Finalizar venda" });

    public ILocator BotaoDeEnvio => pagina.Locator("#nova-venda [type=submit]");

    public ILocator Linha(int indice) => Itens.Nth(indice);

    public Task ProntoAsync() =>
        Expect(pagina.Locator("#nova-venda")).ToHaveAttributeAsync("data-pronto", "true");

    public ILocator ProdutoDaLinha(int indice) =>
        Linha(indice).GetByRole(AriaRole.Combobox, new() { Name = "Produto", Exact = true });

    public ILocator QuantidadeDaLinha(int indice) =>
        Linha(indice).GetByLabel("Quantidade", new() { Exact = true });

    public async Task<ILocator> SugestoesDeAsync(ILocator combobox)
    {
        var lista = await combobox.GetAttributeAsync("aria-controls");
        lista.Should().NotBeNullOrEmpty("o campo de busca aponta a lista de sugestões em aria-controls");
        return pagina.Locator($"[id='{lista}']");
    }

    public async Task EscolherClienteAsync(string termo, string nome)
    {
        await ProntoAsync();
        await EscolherSugestaoAsync(ComboboxDeCliente, termo, nome);
        await Expect(pagina.GetByTestId("cliente-escolhido")).ToContainTextAsync(nome);
    }

    public async Task LancarAsync(int linha, string termo, string produto, string quantidade)
    {
        await ProntoAsync();
        if (await Itens.CountAsync() <= linha)
        {
            await pagina.GetByRole(AriaRole.Button, new() { Name = "Adicionar produto" }).ClickAsync();
        }

        await EscolherSugestaoAsync(ProdutoDaLinha(linha), termo, produto);
        await QuantidadeDaLinha(linha).FillAsync(quantidade);
    }

    private async Task EscolherSugestaoAsync(ILocator combobox, string termo, string sugestao)
    {
        var opcao = pagina.GetByRole(AriaRole.Option).Filter(new() { HasText = sugestao });
        var tentativa = 1;
        while (!await SugeriuAsync(combobox, termo, opcao) && tentativa < TentativasDeBusca)
        {
            tentativa++;
            await combobox.FillAsync(string.Empty);
        }

        await opcao.ClickAsync();
    }

    private static async Task<bool> SugeriuAsync(ILocator combobox, string termo, ILocator opcao)
    {
        await combobox.FillAsync(termo);
        try
        {
            await opcao.WaitForAsync(new() { Timeout = EsperaPelaSugestao });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
