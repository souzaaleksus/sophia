using Sophia.Application.Clientes;

namespace Sophia.Application.Tests.Clientes;

public sealed class BuscaDeClienteTests
{
    [Theory]
    [InlineData("529.982", "529982")]
    [InlineData(" 529 ", "529")]
    [InlineData("52998224725", "52998224725")]
    public void Termo_sem_letras_busca_pelo_cpf(string termo, string inicioDoCpf)
    {
        var busca = BuscaDeCliente.Interpretar(termo);

        busca.InicioDoCpf.Should().Be(inicioDoCpf);
        busca.TrechoDoNome.Should().BeNull();
    }

    [Theory]
    [InlineData("maria", "maria")]
    [InlineData("  João Lima ", "João Lima")]
    public void Termo_com_letras_busca_pelo_nome(string termo, string trecho)
    {
        var busca = BuscaDeCliente.Interpretar(termo);

        busca.TrechoDoNome.Should().Be(trecho);
        busca.InicioDoCpf.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".-")]
    public void Termo_vazio_nao_filtra(string? termo)
    {
        var busca = BuscaDeCliente.Interpretar(termo);

        busca.FiltraAlgo.Should().BeFalse();
    }
}
