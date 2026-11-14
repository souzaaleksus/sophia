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
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task No_desktop_o_tema_e_uma_chave_com_claro_escuro_e_automatico()
    {
        await _pagina.GotoAsync("/Produtos");

        await Expect(GrupoDoTema()).ToBeVisibleAsync();
        await Expect(GrupoDoTema().GetByRole(AriaRole.Radio)).ToHaveCountAsync(3);
        foreach (var opcao in new[] { "Claro", "Escuro", "Automático" })
        {
            await Expect(OpcaoDoTema(opcao)).ToBeVisibleAsync();
        }

        await Expect(OpcaoDoTema("Automático")).ToBeCheckedAsync();
    }

    [Fact]
    public async Task Escolher_o_tema_escuro_escurece_a_pagina_e_a_escolha_sobrevive_ao_recarregamento()
    {
        await _pagina.GotoAsync("/Produtos");
        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "light");

        await Expect(OpcaoDoTema("Escuro")).ToBeVisibleAsync();
        await OpcaoDoTema("Escuro").ClickAsync();

        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "dark");
        await Expect(OpcaoDoTema("Escuro")).ToBeCheckedAsync();
        await _pagina.ReloadAsync();
        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "dark");
        await Expect(OpcaoDoTema("Escuro")).ToBeCheckedAsync();
        await Expect(OpcaoDoTema("Claro")).Not.ToBeCheckedAsync();
    }

    [Fact]
    public async Task As_setas_do_teclado_mudam_o_tema()
    {
        await _pagina.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await _pagina.GotoAsync("/Produtos");
        await Expect(OpcaoDoTema("Automático")).ToBeCheckedAsync();
        await OpcaoDoTema("Automático").FocusAsync();

        await _pagina.Keyboard.PressAsync("ArrowLeft");

        await Expect(OpcaoDoTema("Escuro")).ToBeCheckedAsync();
        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "dark");

        await _pagina.Keyboard.PressAsync("ArrowLeft");

        await Expect(OpcaoDoTema("Claro")).ToBeCheckedAsync();
        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "light");
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

        await Expect(OpcaoDoTema("Claro")).ToBeVisibleAsync();
        await OpcaoDoTema("Claro").ClickAsync();
        await _pagina.ReloadAsync();

        await Expect(Html()).ToHaveAttributeAsync("data-bs-theme", "light");
    }

    [Fact]
    public async Task No_celular_o_tema_continua_no_botao_de_menu()
    {
        var celular = await aplicacao.NovaPaginaAsync(360, 740, toque: true);
        try
        {
            await celular.GotoAsync("/Produtos");
            var botao = celular.GetByRole(AriaRole.Button, new() { NameRegex = NomeDoBotaoDoTema() });
            await Expect(botao).ToBeVisibleAsync();

            await botao.ClickAsync();
            await celular.GetByRole(AriaRole.Button, new() { Name = "Escuro", Exact = true }).ClickAsync();

            await Expect(celular.Locator("html")).ToHaveAttributeAsync("data-bs-theme", "dark");
            await Expect(botao).ToHaveAttributeAsync("aria-label", "Escolher o tema (atual: escuro)");
        }
        finally
        {
            await celular.Context.CloseAsync();
        }
    }

    private ILocator Html() => _pagina.Locator("html");

    private ILocator GrupoDoTema() =>
        _pagina.GetByRole(AriaRole.Radiogroup, new() { Name = "Tema", Exact = true });

    private ILocator OpcaoDoTema(string nome) =>
        GrupoDoTema().GetByRole(AriaRole.Radio, new() { Name = nome, Exact = true });

    [GeneratedRegex("^Escolher o tema")]
    private static partial Regex NomeDoBotaoDoTema();
}
