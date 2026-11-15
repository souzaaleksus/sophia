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
    public async Task O_aviso_de_cpf_repetido_some_quando_o_cpf_e_alterado()
    {
        await CadastrarAsync("529.982.247-25", "Maria Santos");
        await _pagina.GotoAsync("/Clientes/Novo");
        await _pagina.GetByLabel("CPF").FillAsync("529.982.247-25");
        await _pagina.GetByLabel("Nome").FillAsync("Outra Maria");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        var aviso = _pagina.Locator("[data-valmsg-for=Cpf]");
        await Expect(aviso).ToHaveTextAsync("Já existe um cliente com este CPF.");
        await Expect(_pagina.GetByLabel("CPF")).ToHaveAttributeAsync("aria-invalid", "true");

        await _pagina.GetByLabel("CPF").FillAsync("111.444.777-35");

        await Expect(aviso).ToBeEmptyAsync();
        await Expect(_pagina.GetByLabel("CPF")).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory]
    [InlineData("Telefone", "1234", "Informe o telefone com DDD.")]
    [InlineData("Telefone", "(11) 9876-543", "Informe o telefone com DDD.")]
    [InlineData("E-mail", "fulano@dominio", "E-mail inválido.")]
    [InlineData("E-mail", "fulano @dominio.com", "E-mail inválido.")]
    public async Task O_navegador_recusa_ao_sair_do_campo_o_que_o_servidor_recusaria(string rotulo, string digitado, string aviso)
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        await _pagina.GetByLabel("CPF").FillAsync("529.982.247-25");
        await _pagina.GetByLabel("Nome").FillAsync("Maria Santos");
        var campo = _pagina.GetByLabel(rotulo, new() { Exact = true });

        await campo.FillAsync(digitado);
        await campo.BlurAsync();

        await Expect(campo).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(campo).ToHaveAccessibleDescriptionAsync(aviso);
        var envios = 0;
        _pagina.Request += (_, pedido) =>
        {
            if (pedido.Method == "POST" && new Uri(pedido.Url).AbsolutePath.Equals("/Clientes/Novo", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref envios);
            }
        };

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(campo).ToBeFocusedAsync();
        await Expect(campo).ToHaveAccessibleDescriptionAsync(aviso);
        Volatile.Read(ref envios).Should().Be(0, "o navegador segura o envio, sem ida ao servidor");
        (await new Cadastro(aplicacao.Banco).QuantidadeDeClientesAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("(11) 3456-7890", "maria@exemplo.com.br")]
    [InlineData("11987654321", "m@e.co")]
    public async Task Telefone_com_ddd_e_e_mail_completo_passam_no_navegador_e_no_servidor(string telefone, string email)
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        await _pagina.GetByLabel("CPF").FillAsync("529.982.247-25");
        await _pagina.GetByLabel("Nome").FillAsync("Maria Santos");
        await _pagina.GetByLabel("Telefone").FillAsync(telefone);
        await _pagina.GetByLabel("E-mail").FillAsync(email);

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Cliente cadastrado.");
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

        var dialogo = await Exclusao.AbrirDialogoAsync(_pagina, "Maria Santos Silva");
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

        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "João Lima" })).ToHaveCountAsync(0);
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
