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
    public async Task Lista_em_ordem_alfabetica_e_filtra_por_cpf_ou_nome()
    {
        await Servico.CadastrarAsync(Maria());
        await Servico.CadastrarAsync(Maria("111.444.777-35") with { Nome = "João Lima" });

        (await Servico.ListarAsync(null)).Select(c => c.Nome).Should().Equal("João Lima", "Maria Santos");
        (await Servico.ListarAsync("529.982")).Select(c => c.Nome).Should().Equal("Maria Santos");
        (await Servico.ListarAsync("LIMA")).Select(c => c.Nome).Should().Equal("João Lima");
    }
}
