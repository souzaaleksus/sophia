using Sophia.Application.Produtos;
using Sophia.Application.Tests.Apoio;
using Sophia.Domain;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Testes.Banco;

namespace Sophia.Application.Tests.Produtos;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ProdutoServicoTests(BancoDeTestes banco) : IAsyncLifetime, IDisposable
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    private readonly SistemaEmTeste _sistema = new(banco, Hoje);

    private IProdutoServico Servico => _sistema.Produtos;

    public Task InitializeAsync() => banco.LimparAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _sistema.Dispose();

    [Fact]
    public async Task Cadastra_e_devolve_o_identificador()
    {
        var resultado = await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        resultado.Sucesso.Should().BeTrue();
        (await Servico.ObterAsync(resultado.Valor)).Should()
            .Be(new ProdutoDto(resultado.Valor, "Caneta azul", 2.50m));
    }

    [Fact]
    public async Task Recusa_nome_repetido_sem_diferenciar_maiusculas()
    {
        await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        var resultado = await Servico.CadastrarAsync(new DadosProduto("CANETA AZUL", 3m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Converte_a_violacao_de_unicidade_do_banco_na_mesma_mensagem()
    {
        _sistema.AntesDoProximoSalvamento(async () =>
            (await _sistema.Produtos.CadastrarAsync(new DadosProduto("CANETA AZUL", 3m))).Sucesso.Should().BeTrue());

        var resultado = await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Altera_o_valor_mantendo_o_proprio_nome()
    {
        var id = (await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m))).Valor;

        var resultado = await Servico.AlterarAsync(id, new DadosProduto("Caneta azul", 2.75m));

        resultado.Sucesso.Should().BeTrue();
        (await Servico.ObterAsync(id))!.ValorUnitario.Should().Be(2.75m);
    }

    [Fact]
    public async Task Recusa_alteracao_para_o_nome_de_outro_produto()
    {
        await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));
        var lapis = (await Servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;

        var resultado = await Servico.AlterarAsync(lapis, new DadosProduto("Caneta azul", 1m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Avisa_quando_o_produto_nao_existe()
    {
        var alterar = await Servico.AlterarAsync(999, new DadosProduto("Lápis", 1m));
        var excluir = await Servico.ExcluirAsync(999);

        alterar.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Produto não encontrado.");
        excluir.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Produto não encontrado.");
    }

    [Fact]
    public async Task Exclui_produto_nunca_vendido()
    {
        var id = (await Servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;

        (await Servico.ExcluirAsync(id)).Sucesso.Should().BeTrue();
        (await Servico.ObterAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task Nao_exclui_produto_ja_vendido()
    {
        var id = (await Servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;
        var maria = (await _sistema.Clientes.CadastrarAsync(
            new DadosCliente("529.982.247-25", "Maria Santos", null, null, null))).Valor;
        (await _sistema.Vendas.RegistrarAsync(new DadosVenda(Hoje, maria, [new(id, 1, 1m)]))).Sucesso.Should().BeTrue();

        var resultado = await Servico.ExcluirAsync(id);

        resultado.Erros.Should().ContainSingle()
            .Which.Mensagem.Should().Be("Este produto já foi vendido e não pode ser excluído.");
    }

    [Fact]
    public async Task Lista_em_paginas_de_25_em_ordem_alfabetica()
    {
        await CadastrarProdutosAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista());

        lista.Ordem.Should().Be("nome");
        lista.Pagina.Tamanho.Should().Be(25);
        lista.Pagina.TotalDeItens.Should().Be(30);
        lista.Pagina.TotalDePaginas.Should().Be(2);
        lista.Pagina.Itens.Select(p => p.Nome).Should().Equal(Nomes(1, 25));
    }

    [Theory]
    [InlineData(null, "nome", "Produto 01", 30)]
    [InlineData("nome", "nome", "Produto 01", 30)]
    [InlineData("-nome", "-nome", "Produto 30", 1)]
    [InlineData("valor", "valor", "Produto 30", 1)]
    [InlineData("-valor", "-valor", "Produto 01", 30)]
    [InlineData("cpf", "nome", "Produto 01", 30)]
    [InlineData("-total", "nome", "Produto 01", 30)]
    public async Task Ordena_pela_coluna_pedida_e_devolve_a_ordem_que_ficou_valendo(
        string? pedida,
        string valendo,
        string primeiro,
        int valorDoPrimeiro)
    {
        await CadastrarProdutosAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista(Ordem: pedida));

        lista.Ordem.Should().Be(valendo);
        lista.Pagina.Itens[0].Should().BeEquivalentTo(new { Nome = primeiro, ValorUnitario = (decimal)valorDoPrimeiro });
    }

    [Fact]
    public async Task Valores_iguais_ficam_na_ordem_do_cadastro_e_se_invertem_junto_com_a_coluna()
    {
        var borracha = (await Servico.CadastrarAsync(new DadosProduto("Borracha", 1.50m))).Valor;
        var apontador = (await Servico.CadastrarAsync(new DadosProduto("Apontador", 1.50m))).Valor;
        var regua = (await Servico.CadastrarAsync(new DadosProduto("Régua", 4m))).Valor;

        (await Servico.ListarAsync(new ConsultaDeLista(Ordem: "valor"))).Pagina.Itens.Select(p => p.Id)
            .Should().Equal(borracha, apontador, regua);
        (await Servico.ListarAsync(new ConsultaDeLista(Ordem: "-valor"))).Pagina.Itens.Select(p => p.Id)
            .Should().Equal(regua, apontador, borracha);
    }

    [Fact]
    public async Task Filtra_pelo_nome_antes_de_paginar()
    {
        await CadastrarProdutosAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista(Busca: "  produto 2  ", Ordem: "valor"));

        lista.Pagina.TotalDeItens.Should().Be(10);
        lista.Pagina.Itens.Select(p => p.Nome).Should().Equal(Nomes(20, 29).Reverse());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(3, 2)]
    [InlineData(50, 2)]
    public async Task Pagina_fora_dos_limites_vira_a_primeira_ou_a_ultima(int pedida, int mostrada)
    {
        await CadastrarProdutosAsync(30);

        var lista = await Servico.ListarAsync(new ConsultaDeLista(Pagina: pedida));

        lista.Pagina.Numero.Should().Be(mostrada);
        lista.Pagina.Itens.Should().NotBeEmpty();
    }

    [Fact]
    public async Task As_sugestoes_da_busca_respeitam_o_limite_e_a_ordem_alfabetica()
    {
        await CadastrarProdutosAsync(30);

        (await Servico.BuscarAsync("produto", 8)).Select(p => p.Nome).Should().Equal(Nomes(1, 8));
        (await Servico.BuscarAsync("produto 3", 8)).Select(p => p.Nome).Should().Equal("Produto 30");
    }

    private static IEnumerable<string> Nomes(int primeiro, int ultimo) =>
        Enumerable.Range(primeiro, ultimo - primeiro + 1).Select(numero => $"Produto {numero:00}");

    private async Task CadastrarProdutosAsync(int quantidade)
    {
        for (var numero = 1; numero <= quantidade; numero++)
        {
            var resultado = await Servico.CadastrarAsync(new DadosProduto($"Produto {numero:00}", quantidade - numero + 1));
            resultado.Sucesso.Should().BeTrue();
        }
    }
}
