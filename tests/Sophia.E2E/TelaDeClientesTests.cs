using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class TelaDeClientesTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _pagina = await aplicacao.NovaPaginaAsync();
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task Cadastra_o_cliente_e_o_mostra_na_lista()
    {
        await _pagina.GotoAsync("/Clientes");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Novo cliente" }).ClickAsync();

        await _pagina.GetByLabel("CPF").FillAsync("52998224725");
        await _pagina.GetByLabel("Nome").FillAsync("Maria Santos");
        await Expect(_pagina.GetByLabel("Data de nascimento")).ToHaveAttributeAsync("type", "text");
        await _pagina.GetByLabel("Data de nascimento").FillAsync("15/03/1990");
        await _pagina.GetByLabel("E-mail").FillAsync("maria@exemplo.com.br");
        await _pagina.GetByLabel("Telefone").FillAsync("11987654321");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Cliente cadastrado.");
        var linha = _pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Maria Santos" });
        await Expect(linha).ToContainTextAsync("529.982.247-25");
        await Expect(linha).ToContainTextAsync("15/03/1990");
        await Expect(linha).ToContainTextAsync("(11) 98765-4321");
    }

    [Fact]
    public async Task Avisa_no_campo_quando_o_cpf_ja_existe()
    {
        await CadastrarAsync("529.982.247-25", "Maria Santos");

        await _pagina.GotoAsync("/Clientes/Novo");
        await _pagina.GetByLabel("CPF").FillAsync("529.982.247-25");
        await _pagina.GetByLabel("Nome").FillAsync("Outra Maria");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=Cpf]")).ToHaveTextAsync("Já existe um cliente com este CPF.");
    }

    [Fact]
    public async Task Avisa_no_campo_quando_o_cpf_e_invalido()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        await _pagina.GetByLabel("CPF").FillAsync("111.111.111-11");
        await _pagina.GetByLabel("Nome").FillAsync("Ana");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=Cpf]")).ToHaveTextAsync("CPF inválido.");
    }

    [Fact]
    public async Task Edita_e_exclui_o_cliente()
    {
        await CadastrarAsync("529.982.247-25", "Maria Santos");

        await _pagina.GotoAsync("/Clientes");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Editar Maria Santos" }).ClickAsync();
        await _pagina.GetByLabel("Nome").FillAsync("Maria Santos Silva");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Cliente alterado.");

        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Maria Santos Silva" }).ClickAsync();
        var dialogo = _pagina.GetByRole(AriaRole.Dialog);
        await Expect(dialogo).ToContainTextAsync("Maria Santos Silva");
        await dialogo.GetByRole(AriaRole.Button, new() { Name = "Excluir", Exact = true }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Cliente excluído.");
        await Expect(_pagina.GetByText("Nenhum cliente encontrado.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Filtra_a_lista_por_cpf_ou_nome()
    {
        await CadastrarAsync("529.982.247-25", "Maria Santos");
        await CadastrarAsync("111.444.777-35", "João Lima");

        await _pagina.GotoAsync("/Clientes");
        await _pagina.GetByRole(AriaRole.Searchbox).PressSequentiallyAsync("529.982");

        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "João Lima" })).ToHaveCountAsync(0, new() { Timeout = 2000 });
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Maria Santos" })).ToBeVisibleAsync();
    }

    private async Task CadastrarAsync(string cpf, string nome)
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        await _pagina.GetByLabel("CPF").FillAsync(cpf);
        await _pagina.GetByLabel("Nome").FillAsync(nome);
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Cliente cadastrado.");
    }
}
