using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed partial class TemaTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;

    public async Task InitializeAsync()
    {
        _pagina = await aplicacao.NovaPaginaAsync();
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task Escolher_o_tema_escuro_escurece_a_pagina_e_a_escolha_sobrevive_ao_recarregamento()
    {
        await _pagina.GotoAsync("/Produtos");
        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "light");

        await EscolherTemaAsync("Escuro");

        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "dark");
        await _pagina.ReloadAsync();
        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "dark");
        await Expect(BotaoDoTema()).ToHaveAttributeAsync("aria-label", "Escolher o tema (atual: escuro)");
    }

    [Fact]
    public async Task No_tema_automatico_a_pagina_segue_o_sistema()
    {
        await _pagina.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await _pagina.GotoAsync("/Clientes");

        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "dark");

        await _pagina.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });

        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "light");
    }

    [Fact]
    public async Task O_tema_claro_vale_mesmo_com_o_sistema_no_escuro()
    {
        await _pagina.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await _pagina.GotoAsync("/Clientes");

        await EscolherTemaAsync("Claro");
        await _pagina.ReloadAsync();

        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "light");
    }

    private ILocator Html() => _pagina.Locator("html");

    private ILocator BotaoDoTema() =>
        _pagina.GetByRole(AriaRole.Button, new() { NameRegex = NomeDoBotaoDoTema() });

    private async Task EscolherTemaAsync(string tema)
    {
        await BotaoDoTema().ClickAsync();
        await _pagina.GetByRole(AriaRole.Button, new() { Name = tema, Exact = true }).ClickAsync();
    }

    [GeneratedRegex("^Escolher o tema")]
    private static partial Regex NomeDoBotaoDoTema();
}
