namespace Sophia.Domain.Tests;

public sealed class ResultadoTests
{
    [Fact]
    public void Resultado_de_sucesso_nao_tem_erros()
    {
        var resultado = Resultado.Ok(42);

        resultado.Sucesso.Should().BeTrue();
        resultado.Erros.Should().BeEmpty();
        resultado.Valor.Should().Be(42);
    }

    [Fact]
    public void Resultado_de_falha_guarda_os_erros()
    {
        var resultado = Resultado.Falha<int>(new Erro("Nome", "Informe o nome."));

        resultado.Sucesso.Should().BeFalse();
        resultado.Erros.Should().ContainSingle();
    }

    [Fact]
    public void Ler_o_valor_de_uma_falha_e_erro_de_programacao()
    {
        var resultado = Resultado.Falha<int>(new Erro("Nome", "Informe o nome."));

        var ler = () => resultado.Valor;

        ler.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Falha_sem_erros_nao_existe()
    {
        var criar = () => Resultado.Falha<int>();

        criar.Should().Throw<ArgumentException>();
    }
}
