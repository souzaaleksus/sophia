using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed partial class ControlesDeFormularioTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private const string Controles = "main input:not([type=hidden]), main button, main a.btn";
    private const int AlturaMinima = 44;

    private IPage _pagina = null!;
    private Cadastro _cadastro = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _cadastro = new Cadastro(aplicacao.Banco);
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task Apagar_um_separador_da_mascara_apaga_o_digito_antes_dele_e_mantem_o_cursor()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        var cpf = _pagina.GetByLabel("CPF");
        await cpf.PressSequentiallyAsync("52998224725");
        await Expect(cpf).ToHaveValueAsync("529.982.247-25");

        await PosicionarAsync(cpf, 4);
        await cpf.PressAsync("Backspace");

        await Expect(cpf).ToHaveValueAsync("529.822.472-5");
        (await CursorAsync(cpf)).Should().Be(2);
    }

    [Fact]
    public async Task Digitar_no_meio_da_mascara_mantem_o_cursor_e_campo_cheio_nao_perde_digito()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        var cpf = _pagina.GetByLabel("CPF");
        await cpf.PressSequentiallyAsync("5299822472");

        await PosicionarAsync(cpf, 4);
        await cpf.PressSequentiallyAsync("1");

        await Expect(cpf).ToHaveValueAsync("529.198.224-72");
        (await CursorAsync(cpf)).Should().Be(5);

        await PosicionarAsync(cpf, 1);
        await cpf.PressSequentiallyAsync("7");

        await Expect(cpf).ToHaveValueAsync("529.198.224-72");
        (await CursorAsync(cpf)).Should().Be(1);
    }

    [Theory]
    [InlineData("Telefone", "+55 11 98765-4321", "(11) 98765-4321")]
    [InlineData("Telefone", "+55 (11) 3456-7890", "(11) 3456-7890")]
    [InlineData("Data de nascimento", "1990-05-12", "12/05/1990")]
    public async Task Valor_colado_ou_preenchido_pelo_navegador_e_entendido_pela_mascara(string campo, string colado, string esperado)
    {
        await _pagina.GotoAsync("/Clientes/Novo");

        await _pagina.GetByLabel(campo).FillAsync(colado);

        await Expect(_pagina.GetByLabel(campo)).ToHaveValueAsync(esperado);
    }

    private static async Task PosicionarAsync(ILocator campo, int posicao) =>
        await campo.EvaluateAsync("(campo, posicao) => { campo.focus(); campo.setSelectionRange(posicao, posicao); }", posicao);

    private static Task<int> CursorAsync(ILocator campo) =>
        campo.EvaluateAsync<int>("campo => campo.selectionStart");

    [Fact]
    public async Task O_novo_cliente_abre_com_o_foco_no_cpf()
    {
        await _pagina.GotoAsync("/Clientes/Novo");

        await Expect(_pagina.GetByLabel("CPF")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Editar_o_cliente_abre_com_o_foco_no_primeiro_campo()
    {
        var id = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");

        await _pagina.GotoAsync($"/Clientes/Editar/{id}");

        await Expect(_pagina.GetByLabel("CPF")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Cpf_e_nome_do_cliente_sao_obrigatorios_e_marcados_com_asterisco()
    {
        await _pagina.GotoAsync("/Clientes/Novo");

        foreach (var (campo, rotulo) in new[] { ("CPF", "label[for=Cpf]"), ("Nome", "label[for=Nome]") })
        {
            await Expect(_pagina.GetByLabel(campo)).ToHaveAttributeAsync("aria-required", "true");
            await Expect(_pagina.Locator(rotulo)).ToBeVisibleAsync();
            await Expect(_pagina.Locator(rotulo)).ToContainTextAsync("*");
        }

        await Expect(_pagina.Locator("main form").GetByText("* obrigatório")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task O_novo_produto_abre_com_o_foco_no_nome_e_marca_os_obrigatorios()
    {
        await _pagina.GotoAsync("/Produtos/Novo");

        await Expect(_pagina.GetByLabel("Nome")).ToBeFocusedAsync();
        await Expect(_pagina.GetByLabel("Nome")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(_pagina.GetByLabel("Valor unitário")).ToHaveAttributeAsync("aria-required", "true");
    }

    [Theory]
    [InlineData("/Clientes/Novo")]
    [InlineData("/Clientes/Editar/{cliente}")]
    [InlineData("/Produtos/Novo")]
    [InlineData("/Produtos/Editar/{produto}")]
    public async Task No_desktop_todo_controle_do_formulario_tem_ao_menos_44_de_altura(string endereco)
    {
        var cliente = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        var produto = await _cadastro.ProdutoAsync("Caneta azul", 2.50m);

        await _pagina.GotoAsync(endereco
            .Replace("{cliente}", cliente.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{produto}", produto.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));

        var baixos = await Medidas.AlturasMenoresQueAsync(_pagina, Controles, AlturaMinima);
        baixos.Should().BeEmpty($"em {endereco} todo controle precisa de ao menos {AlturaMinima} px de altura");
    }

    [Fact]
    public async Task A_data_de_nascimento_e_texto_com_mascara_de_dia_mes_e_ano()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        var nascimento = _pagina.GetByLabel("Data de nascimento");

        await Expect(nascimento).ToHaveAttributeAsync("type", "text");
        await Expect(nascimento).ToHaveAttributeAsync("placeholder", "dd/mm/aaaa");
        await nascimento.PressSequentiallyAsync("12051990");

        await Expect(nascimento).ToHaveValueAsync("12/05/1990");
    }

    [Fact]
    public async Task A_data_de_nascimento_digitada_e_gravada_e_aparece_na_lista()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        await Expect(_pagina.GetByLabel("Data de nascimento")).ToHaveAttributeAsync("type", "text");

        await _pagina.GetByLabel("CPF").FillAsync("529.982.247-25");
        await _pagina.GetByLabel("Nome").FillAsync("Maria Santos");
        await _pagina.GetByLabel("Data de nascimento").PressSequentiallyAsync("12051990");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Cliente cadastrado.");
        var linha = _pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Maria Santos" });
        var nascimento = _pagina.GetByRole(AriaRole.Columnheader, new() { Name = "Nascimento" });
        await Expect(nascimento).ToBeVisibleAsync();
        await Expect(linha).ToContainTextAsync("12/05/1990");
    }

    [Fact]
    public async Task A_data_de_nascimento_invalida_avisa_e_nao_grava()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        await Expect(_pagina.GetByLabel("Data de nascimento")).ToHaveAttributeAsync("type", "text");

        await _pagina.GetByLabel("CPF").FillAsync("529.982.247-25");
        await _pagina.GetByLabel("Nome").FillAsync("Maria Santos");
        await _pagina.GetByLabel("Data de nascimento").FillAsync("31/02/1990");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.GetByText("Informe uma data de nascimento válida.")).ToBeVisibleAsync();
        (await _cadastro.QuantidadeDeClientesAsync()).Should().Be(0);
    }

    [Fact]
    public async Task O_cpf_com_digito_errado_avisa_ao_sair_do_campo_sem_enviar_o_formulario()
    {
        var envios = new ConcurrentQueue<string>();
        _pagina.Request += (_, requisicao) =>
        {
            if (requisicao.Method == "POST" && requisicao.Url.StartsWith(aplicacao.Endereco, StringComparison.Ordinal))
            {
                envios.Enqueue(requisicao.Url);
            }
        };
        await _pagina.GotoAsync("/Clientes/Novo");
        var cpf = _pagina.GetByLabel("CPF");

        await cpf.FillAsync("111.444.777-34");
        await cpf.PressAsync("Tab");

        await Expect(_pagina.GetByText("CPF inválido.")).ToBeVisibleAsync();
        await Expect(cpf).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(_pagina).ToHaveURLAsync(EnderecoDoNovoCliente());
        envios.Should().BeEmpty("sair do campo não envia o formulário");
        (await _cadastro.QuantidadeDeClientesAsync()).Should().Be(0);

        await cpf.FillAsync("111.444.777-35");
        await cpf.PressAsync("Tab");

        await Expect(_pagina.GetByText("CPF inválido.")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Logo_apos_salvar_o_botao_fica_desabilitado_mostrando_salvando()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        await _pagina.GetByLabel("CPF").FillAsync("529.982.247-25");
        await _pagina.GetByLabel("Nome").FillAsync("Maria Santos");
        await _pagina.EvaluateAsync("() => window.addEventListener('submit', evento => evento.preventDefault())");
        var envio = _pagina.Locator("main form [type=submit]");

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(envio).ToBeDisabledAsync();
        await Expect(envio).ToContainTextAsync("Salvando…");
        (await _cadastro.QuantidadeDeClientesAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Corrigir_o_campo_com_erro_some_com_a_mensagem_sem_deslocar_o_botao_de_salvar()
    {
        var nome = await ProvocarNomeRepetidoAsync();
        var salvar = _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" });
        var antes = (await salvar.BoundingBoxAsync())!.Y;

        await nome.FillAsync("Caneta vermelha");
        await nome.BlurAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=Nome]")).ToBeEmptyAsync();
        (await salvar.BoundingBoxAsync())!.Y.Should().Be(antes);
    }

    [Fact]
    public async Task Depois_de_corrigir_o_erro_um_clique_em_salvar_basta()
    {
        var nome = await ProvocarNomeRepetidoAsync();

        await nome.FillAsync("Caneta vermelha");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Produto cadastrado.");
        (await _cadastro.QuantidadeDeProdutosAsync()).Should().Be(2);
    }

    private async Task<ILocator> ProvocarNomeRepetidoAsync()
    {
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _pagina.GotoAsync("/Produtos/Novo");
        var nome = _pagina.GetByLabel("Nome");
        await nome.FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync("3,00");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        await Expect(_pagina.Locator("[data-valmsg-for=Nome]")).ToHaveTextAsync("Já existe um produto com este nome.");
        return nome;
    }

    [GeneratedRegex(@"/Clientes/Novo$")]
    private static partial Regex EnderecoDoNovoCliente();
}
