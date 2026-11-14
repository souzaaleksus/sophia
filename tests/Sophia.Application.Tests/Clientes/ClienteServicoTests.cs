using Sophia.Application.Clientes;
using Sophia.Domain;
using Sophia.Domain.Clientes;
using Sophia.Tests.Fakes;

namespace Sophia.Application.Tests.Clientes;

public sealed class ClienteServicoTests
{
    private readonly BancoEmMemoria _banco = new();
    private readonly ClienteServico _servico;

    public ClienteServicoTests()
    {
        _servico = new ClienteServico(_banco.Clientes, _banco, new RelogioFixo(new DateOnly(2026, 11, 13)));
    }

    private static DadosCliente Maria(string cpf = "529.982.247-25") =>
        new(cpf, "Maria Santos", new DateOnly(1990, 3, 15), "maria@exemplo.com.br", "11987654321");

    [Fact]
    public async Task Cadastra_e_devolve_o_identificador()
    {
        var resultado = await _servico.CadastrarAsync(Maria());

        resultado.Sucesso.Should().BeTrue();
        var cliente = await _servico.ObterAsync(resultado.Valor);
        cliente.Should().BeEquivalentTo(new ClienteDto(
            resultado.Valor, "529.982.247-25", "Maria Santos", new DateOnly(1990, 3, 15),
            "maria@exemplo.com.br", "(11) 98765-4321"));
    }

    [Fact]
    public async Task Recusa_cpf_ja_cadastrado()
    {
        await _servico.CadastrarAsync(Maria());

        var resultado = await _servico.CadastrarAsync(Maria("52998224725") with { Nome = "Outra Maria" });

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Cpf", "Já existe um cliente com este CPF."));
    }

    [Fact]
    public async Task Converte_a_violacao_de_unicidade_do_banco_na_mesma_mensagem()
    {
        _banco.SimularCadastroConcorrenteNoProximoSalvamento();

        var resultado = await _servico.CadastrarAsync(Maria());

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Cpf", "Já existe um cliente com este CPF."));
    }

    [Fact]
    public async Task Nao_consulta_duplicidade_quando_os_dados_ja_sao_invalidos()
    {
        var resultado = await _servico.CadastrarAsync(new DadosCliente("", "", null, null, null));

        resultado.Erros.Select(e => e.Campo).Should().BeEquivalentTo("Cpf", "Nome");
    }

    [Fact]
    public async Task Altera_mantendo_o_proprio_cpf()
    {
        var id = (await _servico.CadastrarAsync(Maria())).Valor;

        var resultado = await _servico.AlterarAsync(id, Maria() with { Nome = "Maria Santos Silva" });

        resultado.Sucesso.Should().BeTrue();
        (await _servico.ObterAsync(id))!.Nome.Should().Be("Maria Santos Silva");
    }

    [Fact]
    public async Task Recusa_alteracao_para_o_cpf_de_outro_cliente()
    {
        await _servico.CadastrarAsync(Maria());
        var joao = (await _servico.CadastrarAsync(Maria("111.444.777-35") with { Nome = "João Lima" })).Valor;

        var resultado = await _servico.AlterarAsync(joao, Maria() with { Nome = "João Lima" });

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Cpf", "Já existe um cliente com este CPF."));
    }

    [Fact]
    public async Task Avisa_quando_o_cliente_nao_existe()
    {
        var alterar = await _servico.AlterarAsync(999, Maria());
        var excluir = await _servico.ExcluirAsync(999);

        alterar.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Cliente não encontrado.");
        excluir.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Cliente não encontrado.");
    }

    [Fact]
    public async Task Exclui_cliente_sem_vendas()
    {
        var id = (await _servico.CadastrarAsync(Maria())).Valor;

        var resultado = await _servico.ExcluirAsync(id);

        resultado.Sucesso.Should().BeTrue();
        (await _servico.ObterAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task Nao_exclui_cliente_com_vendas()
    {
        var id = (await _servico.CadastrarAsync(Maria())).Valor;
        _banco.RegistrarVendaPara(id);

        var resultado = await _servico.ExcluirAsync(id);

        resultado.Erros.Should().ContainSingle()
            .Which.Mensagem.Should().Be("Este cliente possui vendas e não pode ser excluído.");
    }

    [Fact]
    public async Task Lista_em_ordem_alfabetica_e_filtra_por_cpf_ou_nome()
    {
        await _servico.CadastrarAsync(Maria());
        await _servico.CadastrarAsync(Maria("111.444.777-35") with { Nome = "João Lima" });

        (await _servico.ListarAsync(null)).Select(c => c.Nome).Should().Equal("João Lima", "Maria Santos");
        (await _servico.ListarAsync("529.982")).Select(c => c.Nome).Should().Equal("Maria Santos");
        (await _servico.ListarAsync("LIMA")).Select(c => c.Nome).Should().Equal("João Lima");
    }
}
