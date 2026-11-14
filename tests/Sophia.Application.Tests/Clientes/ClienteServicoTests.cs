using Sophia.Application.Clientes;
using Sophia.Application.Tests.Apoio;
using Sophia.Domain;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Testes.Banco;

namespace Sophia.Application.Tests.Clientes;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ClienteServicoTests(BancoDeTestes banco) : IAsyncLifetime, IDisposable
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    private readonly SistemaEmTeste _sistema = new(banco, Hoje);

    private IClienteServico Servico => _sistema.Clientes;

    public Task InitializeAsync() => banco.LimparAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _sistema.Dispose();

    private static DadosCliente Maria(string cpf = "529.982.247-25") =>
        new(cpf, "Maria Santos", new DateOnly(1990, 3, 15), "maria@exemplo.com.br", "11987654321");

    [Fact]
    public async Task Cadastra_e_devolve_o_identificador()
    {
        var resultado = await Servico.CadastrarAsync(Maria());

        resultado.Sucesso.Should().BeTrue();
        var cliente = await Servico.ObterAsync(resultado.Valor);
        cliente.Should().BeEquivalentTo(new ClienteDto(
            resultado.Valor, "529.982.247-25", "Maria Santos", new DateOnly(1990, 3, 15),
            "maria@exemplo.com.br", "(11) 98765-4321"));
    }

    [Fact]
    public async Task Recusa_cpf_ja_cadastrado()
    {
        await Servico.CadastrarAsync(Maria());

        var resultado = await Servico.CadastrarAsync(Maria("52998224725") with { Nome = "Outra Maria" });

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Cpf", "Já existe um cliente com este CPF."));
    }

    [Fact]
    public async Task Converte_a_violacao_de_unicidade_do_banco_na_mesma_mensagem()
    {
        _sistema.AntesDoProximoSalvamento(async () =>
            (await _sistema.Clientes.CadastrarAsync(Maria() with { Nome = "Maria Concorrente" })).Sucesso.Should().BeTrue());

        var resultado = await Servico.CadastrarAsync(Maria());

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Cpf", "Já existe um cliente com este CPF."));
    }

    [Fact]
    public async Task Nao_consulta_duplicidade_quando_os_dados_ja_sao_invalidos()
    {
        var resultado = await Servico.CadastrarAsync(new DadosCliente("", "", null, null, null));

        resultado.Erros.Select(e => e.Campo).Should().BeEquivalentTo("Cpf", "Nome");
    }

    [Fact]
    public async Task Altera_mantendo_o_proprio_cpf()
    {
        var id = (await Servico.CadastrarAsync(Maria())).Valor;

        var resultado = await Servico.AlterarAsync(id, Maria() with { Nome = "Maria Santos Silva" });

        resultado.Sucesso.Should().BeTrue();
        (await Servico.ObterAsync(id))!.Nome.Should().Be("Maria Santos Silva");
    }

    [Fact]
    public async Task Recusa_alteracao_para_o_cpf_de_outro_cliente()
    {
        await Servico.CadastrarAsync(Maria());
        var joao = (await Servico.CadastrarAsync(Maria("111.444.777-35") with { Nome = "João Lima" })).Valor;

        var resultado = await Servico.AlterarAsync(joao, Maria() with { Nome = "João Lima" });

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Cpf", "Já existe um cliente com este CPF."));
    }

    [Fact]
    public async Task Avisa_quando_o_cliente_nao_existe()
    {
        var alterar = await Servico.AlterarAsync(999, Maria());
        var excluir = await Servico.ExcluirAsync(999);

        alterar.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Cliente não encontrado.");
        excluir.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Cliente não encontrado.");
    }

    [Fact]
    public async Task Exclui_cliente_sem_vendas()
    {
        var id = (await Servico.CadastrarAsync(Maria())).Valor;

        var resultado = await Servico.ExcluirAsync(id);

        resultado.Sucesso.Should().BeTrue();
        (await Servico.ObterAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task Nao_exclui_cliente_com_vendas()
    {
        var id = (await Servico.CadastrarAsync(Maria())).Valor;
        var caneta = (await _sistema.Produtos.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m))).Valor;
        (await _sistema.Vendas.RegistrarAsync(new DadosVenda(Hoje, id, [new(caneta, 1, 2.50m)]))).Sucesso.Should().BeTrue();

        var resultado = await Servico.ExcluirAsync(id);

        resultado.Erros.Should().ContainSingle()
            .Which.Mensagem.Should().Be("Este cliente possui vendas e não pode ser excluído.");
    }

    [Fact]
    public async Task Lista_em_paginas_de_25_em_ordem_alfabetica()
    {
        await CadastrarClientesAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista());

        lista.Ordem.Should().Be("nome");
        lista.Pagina.Numero.Should().Be(1);
        lista.Pagina.Tamanho.Should().Be(new ParametrosDoSophia().TamanhoDaPagina).And.Be(25);
        lista.Pagina.TotalDeItens.Should().Be(30);
        lista.Pagina.Itens.Select(c => c.Nome).Should().Equal(Nomes(1, 25));
    }

    [Fact]
    public async Task A_segunda_pagina_traz_os_clientes_que_sobraram()
    {
        await CadastrarClientesAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista(Pagina: 2));

        lista.Pagina.Numero.Should().Be(2);
        lista.Pagina.Itens.Select(c => c.Nome).Should().Equal(Nomes(26, 30));
        lista.Pagina.TemProxima.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "nome", "Cliente 01", "Cliente 25")]
    [InlineData("nome", "nome", "Cliente 01", "Cliente 25")]
    [InlineData("-nome", "-nome", "Cliente 30", "Cliente 06")]
    [InlineData("cpf", "cpf", "Cliente 30", "Cliente 06")]
    [InlineData("-cpf", "-cpf", "Cliente 01", "Cliente 25")]
    [InlineData("valor", "nome", "Cliente 01", "Cliente 25")]
    [InlineData("-NOME", "nome", "Cliente 01", "Cliente 25")]
    public async Task Ordena_pela_coluna_pedida_e_devolve_a_ordem_que_ficou_valendo(
        string? pedida,
        string valendo,
        string primeiro,
        string ultimo)
    {
        await CadastrarClientesAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista(Ordem: pedida));

        lista.Ordem.Should().Be(valendo);
        lista.Pagina.Itens[0].Nome.Should().Be(primeiro);
        lista.Pagina.Itens[^1].Nome.Should().Be(ultimo);
    }

    [Fact]
    public async Task Nomes_iguais_ficam_na_ordem_do_cadastro_e_se_invertem_junto_com_a_coluna()
    {
        var primeira = (await Servico.CadastrarAsync(Maria(GeradorDeCpf.Gerar(1)) with { Nome = "Ana Souza" })).Valor;
        var segunda = (await Servico.CadastrarAsync(Maria(GeradorDeCpf.Gerar(2)) with { Nome = "Ana Souza" })).Valor;

        (await Servico.ListarAsync(new ConsultaDeLista(Ordem: "nome"))).Pagina.Itens.Select(c => c.Id)
            .Should().Equal(primeira, segunda);
        (await Servico.ListarAsync(new ConsultaDeLista(Ordem: "-nome"))).Pagina.Itens.Select(c => c.Id)
            .Should().Equal(segunda, primeira);
    }

    [Fact]
    public async Task Filtra_por_cpf_ou_nome_antes_de_paginar()
    {
        await Servico.CadastrarAsync(Maria());
        await Servico.CadastrarAsync(Maria("111.444.777-35") with { Nome = "João Lima" });

        (await Servico.ListarAsync(new ConsultaDeLista(Busca: "529.982"))).Pagina.Itens.Select(c => c.Nome)
            .Should().Equal("Maria Santos");
        (await Servico.ListarAsync(new ConsultaDeLista(Busca: "LIMA"))).Pagina.Itens.Select(c => c.Nome)
            .Should().Equal("João Lima");
    }

    [Fact]
    public async Task A_contagem_e_as_paginas_valem_so_para_os_encontrados()
    {
        await CadastrarClientesAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista(Busca: "cliente 1", Ordem: "-nome"));

        lista.Pagina.TotalDeItens.Should().Be(10);
        lista.Pagina.TotalDePaginas.Should().Be(1);
        lista.Pagina.Itens.Select(c => c.Nome).Should().Equal(Nomes(10, 19).Reverse());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(3, 2)]
    [InlineData(99, 2)]
    public async Task Pagina_fora_dos_limites_vira_a_primeira_ou_a_ultima(int pedida, int mostrada)
    {
        await CadastrarClientesAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista(Pagina: pedida));

        lista.Pagina.Numero.Should().Be(mostrada);
        lista.Pagina.Itens.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Sem_clientes_devolve_uma_pagina_vazia()
    {
        var lista = await Servico.ListarAsync(new ConsultaDeLista(Pagina: 4));

        lista.Pagina.Numero.Should().Be(1);
        lista.Pagina.Itens.Should().BeEmpty();
        lista.Pagina.TotalDeItens.Should().Be(0);
    }

    [Fact]
    public async Task As_sugestoes_da_busca_respeitam_o_limite_e_a_ordem_alfabetica()
    {
        await CadastrarClientesAsync(30);

        (await Servico.BuscarAsync("cliente", 8)).Select(c => c.Nome).Should().Equal(Nomes(1, 8));
        (await Servico.BuscarAsync(GeradorDeCpf.Gerar(30), 8)).Select(c => c.Nome).Should().Equal("Cliente 01");
    }

    private static IEnumerable<string> Nomes(int primeiro, int ultimo) =>
        Enumerable.Range(primeiro, ultimo - primeiro + 1).Select(numero => $"Cliente {numero:00}");

    private async Task CadastrarClientesAsync(int quantidade)
    {
        for (var numero = 1; numero <= quantidade; numero++)
        {
            var cpf = GeradorDeCpf.Gerar(quantidade - numero + 1);
            var resultado = await Servico.CadastrarAsync(new DadosCliente(cpf, $"Cliente {numero:00}", null, null, null));
            resultado.Sucesso.Should().BeTrue();
        }
    }
}
