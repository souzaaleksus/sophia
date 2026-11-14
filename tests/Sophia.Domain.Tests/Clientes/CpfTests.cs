using System.Reflection;
using Sophia.Domain.Clientes;

namespace Sophia.Domain.Tests.Clientes;

public sealed class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData(" 111.444.777-35 ", "11144477735")]
    public void Aceita_cpf_valido_com_ou_sem_mascara_e_guarda_so_os_digitos(string digitado, string esperado)
    {
        var resultado = Cpf.Criar(digitado);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.Numero.Should().Be(esperado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Exige_o_cpf(string? digitado)
    {
        var resultado = Cpf.Criar(digitado);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Cpf", "Informe o CPF."));
    }

    [Theory]
    [InlineData("529.982.247-24")]
    [InlineData("529.982.247-15")]
    [InlineData("111.111.111-11")]
    [InlineData("000.000.000-00")]
    [InlineData("5299822472")]
    [InlineData("529982247255")]
    [InlineData("abc.def.ghi-jk")]
    public void Recusa_cpf_invalido(string digitado)
    {
        var resultado = Cpf.Criar(digitado);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Cpf", "CPF inválido."));
    }

    [Fact]
    public void Exibe_o_cpf_formatado()
    {
        var cpf = Cpf.Criar("52998224725").Valor;

        cpf.Formatado.Should().Be("529.982.247-25");
        cpf.ToString().Should().Be("529.982.247-25");
    }

    [Fact]
    public void Dois_cpfs_com_os_mesmos_digitos_sao_iguais()
    {
        Cpf.Criar("529.982.247-25").Valor.Should().Be(Cpf.Criar("52998224725").Valor);
    }

    [Theory]
    [InlineData("52998224724", "529.982.247-24")]
    [InlineData("11111111111", "111.111.111-11")]
    [InlineData("52998224725", "529.982.247-25")]
    public void Reconstitui_do_banco_sem_conferir_os_digitos_verificadores(string gravado, string formatado)
    {
        var cpf = Cpf.DoBanco(gravado);

        cpf.Numero.Should().Be(gravado);
        cpf.Formatado.Should().Be(formatado);
    }

    [Fact]
    public void O_cpf_reconstituido_do_banco_e_igual_ao_criado_com_os_mesmos_digitos()
    {
        Cpf.DoBanco("52998224725").Should().Be(Cpf.Criar("529.982.247-25").Valor);
    }

    [Fact]
    public void Reconstituir_sem_conferir_os_digitos_nao_faz_parte_do_contrato_publico()
    {
        var doBanco = typeof(Cpf).GetMethod(nameof(Cpf.DoBanco), BindingFlags.Static | BindingFlags.NonPublic);

        doBanco.Should().NotBeNull();
        doBanco!.IsAssembly.Should().BeTrue();
    }

    [Theory]
    [InlineData("529.982", "529982")]
    [InlineData("Maria", "")]
    [InlineData(null, "")]
    public void Extrai_somente_os_digitos(string? texto, string esperado)
    {
        Cpf.SomenteDigitos(texto).Should().Be(esperado);
    }
}
