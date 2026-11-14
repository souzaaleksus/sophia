namespace Sophia.Application.Tests;

public sealed class PaginaTests
{
    [Fact]
    public void Primeira_pagina_cheia_tem_proxima_e_nao_tem_anterior()
    {
        var pagina = new Pagina<int>(Enumerable.Range(1, 25).ToList(), 1, 25, 60);

        pagina.TotalDePaginas.Should().Be(3);
        pagina.TemAnterior.Should().BeFalse();
        pagina.TemProxima.Should().BeTrue();
        pagina.PrimeiroItem.Should().Be(1);
        pagina.UltimoItem.Should().Be(25);
    }

    [Fact]
    public void Pagina_do_meio_tem_anterior_e_proxima()
    {
        var pagina = new Pagina<int>(Enumerable.Range(26, 25).ToList(), 2, 25, 60);

        pagina.TemAnterior.Should().BeTrue();
        pagina.TemProxima.Should().BeTrue();
        pagina.PrimeiroItem.Should().Be(26);
        pagina.UltimoItem.Should().Be(50);
    }

    [Fact]
    public void Ultima_pagina_incompleta_mostra_as_posicoes_que_sobraram()
    {
        var pagina = new Pagina<int>(Enumerable.Range(51, 10).ToList(), 3, 25, 60);

        pagina.TemAnterior.Should().BeTrue();
        pagina.TemProxima.Should().BeFalse();
        pagina.PrimeiroItem.Should().Be(51);
        pagina.UltimoItem.Should().Be(60);
    }

    [Theory]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(50, 2)]
    [InlineData(51, 3)]
    public void Conta_as_paginas_arredondando_para_cima(int totalDeItens, int totalDePaginas)
    {
        new Pagina<int>([], 1, 25, totalDeItens).TotalDePaginas.Should().Be(totalDePaginas);
    }

    [Fact]
    public void Sem_itens_ha_uma_pagina_vazia_sem_posicoes()
    {
        var pagina = new Pagina<int>([], 1, 25, 0);

        pagina.TotalDePaginas.Should().Be(1);
        pagina.TemAnterior.Should().BeFalse();
        pagina.TemProxima.Should().BeFalse();
        pagina.PrimeiroItem.Should().Be(0);
        pagina.UltimoItem.Should().Be(0);
    }
}
