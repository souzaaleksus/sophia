using Sophia.Domain.Clientes;

namespace Sophia.Domain.Tests.Clientes;

public sealed class ClienteTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    private static DadosCliente DadosValidos() => new(
        Cpf: "529.982.247-25",
        Nome: "Maria Santos",
        DataNascimento: new DateOnly(1990, 3, 15),
        Email: "maria@exemplo.com.br",
        Telefone: "(11) 98765-4321");

    [Fact]
    public void Cria_o_cliente_com_os_dados_normalizados()
    {
        var resultado = Cliente.Criar(DadosValidos() with { Nome = "  Maria Santos  " }, Hoje);

        resultado.Sucesso.Should().BeTrue();
        var cliente = resultado.Valor;
        cliente.Cpf.Numero.Should().Be("52998224725");
        cliente.Nome.Should().Be("Maria Santos");
        cliente.DataNascimento.Should().Be(new DateOnly(1990, 3, 15));
        cliente.Email.Should().Be("maria@exemplo.com.br");
        cliente.Telefone.Should().Be("11987654321");
    }

    [Fact]
    public void Aceita_somente_cpf_e_nome()
    {
        var resultado = Cliente.Criar(new DadosCliente("52998224725", "João Lima", null, " ", ""), Hoje);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.DataNascimento.Should().BeNull();
        resultado.Valor.Email.Should().BeNull();
        resultado.Valor.Telefone.Should().BeNull();
    }

    [Fact]
    public void Reune_todos_os_erros_de_uma_vez()
    {
        var dados = new DadosCliente("123", "", Hoje.AddDays(1), "sem-arroba", "99");

        var resultado = Cliente.Criar(dados, Hoje);

        resultado.Erros.Select(e => e.Campo).Should()
            .BeEquivalentTo("Cpf", "Nome", "DataNascimento", "Email", "Telefone");
    }

    [Fact]
    public void Recusa_nome_maior_que_150_caracteres()
    {
        var resultado = Cliente.Criar(DadosValidos() with { Nome = new string('a', 151) }, Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "O nome deve ter no máximo 150 caracteres."));
    }

    [Fact]
    public void Aceita_quem_nasceu_hoje_e_recusa_quem_nasce_amanha()
    {
        Cliente.Criar(DadosValidos() with { DataNascimento = Hoje }, Hoje).Sucesso.Should().BeTrue();

        Cliente.Criar(DadosValidos() with { DataNascimento = Hoje.AddDays(1) }, Hoje).Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("DataNascimento", "A data de nascimento não pode estar no futuro."));
    }

    [Fact]
    public void Recusa_data_de_nascimento_anterior_a_1900()
    {
        var resultado = Cliente.Criar(DadosValidos() with { DataNascimento = new DateOnly(1899, 12, 31) }, Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("DataNascimento", "Informe uma data de nascimento válida."));
    }

    [Theory]
    [InlineData("ana@")]
    [InlineData("ana@exemplo")]
    [InlineData("ana exemplo@x.com")]
    [InlineData("@exemplo.com")]
    public void Recusa_email_invalido(string email)
    {
        var resultado = Cliente.Criar(DadosValidos() with { Email = email }, Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Email", "E-mail inválido."));
    }

    [Theory]
    [InlineData("(11) 3456-7890", "1134567890")]
    [InlineData("11 98765 4321", "11987654321")]
    public void Aceita_telefone_fixo_ou_celular_com_ddd(string telefone, string esperado)
    {
        var resultado = Cliente.Criar(DadosValidos() with { Telefone = telefone }, Hoje);

        resultado.Valor.Telefone.Should().Be(esperado);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("98765-4321")]
    [InlineData("119876543210")]
    public void Recusa_telefone_sem_ddd_ou_com_digitos_a_mais(string telefone)
    {
        var resultado = Cliente.Criar(DadosValidos() with { Telefone = telefone }, Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Telefone", "Informe o telefone com DDD."));
    }

    [Fact]
    public void Altera_os_dados_mantendo_a_identidade()
    {
        var cliente = Cliente.Criar(DadosValidos(), Hoje).Valor;

        var resultado = cliente.Alterar(DadosValidos() with { Nome = "Maria Santos Silva", Cpf = "111.444.777-35" }, Hoje);

        resultado.Sucesso.Should().BeTrue();
        cliente.Nome.Should().Be("Maria Santos Silva");
        cliente.Cpf.Numero.Should().Be("11144477735");
    }

    [Fact]
    public void Alteracao_invalida_nao_muda_nada()
    {
        var cliente = Cliente.Criar(DadosValidos(), Hoje).Valor;

        var resultado = cliente.Alterar(DadosValidos() with { Nome = "" }, Hoje);

        resultado.Sucesso.Should().BeFalse();
        cliente.Nome.Should().Be("Maria Santos");
    }
}
