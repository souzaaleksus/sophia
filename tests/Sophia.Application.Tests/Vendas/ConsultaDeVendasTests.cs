using Sophia.Application.Vendas;

namespace Sophia.Application.Tests.Vendas;

public sealed class ConsultaDeVendasTests
{
    private static readonly DateOnly Primeiro = new(2026, 11, 1);
    private static readonly DateOnly Dez = new(2026, 11, 10);

    [Fact]
    public void Sem_parametros_pede_a_primeira_pagina_sem_periodo_e_sem_ordem()
    {
        var consulta = new ConsultaDeVendas();

        consulta.Pagina.Should().Be(1);
        consulta.Inicio.Should().BeNull();
        consulta.Fim.Should().BeNull();
        consulta.Ordem.Should().BeNull();
    }

    [Fact]
    public void Periodo_em_ordem_fica_como_veio()
    {
        var consulta = new ConsultaDeVendas(De: Primeiro, Ate: Dez);

        consulta.Inicio.Should().Be(Primeiro);
        consulta.Fim.Should().Be(Dez);
    }

    [Fact]
    public void Periodo_com_o_inicio_depois_do_fim_troca_as_duas_datas()
    {
        var consulta = new ConsultaDeVendas(De: Dez, Ate: Primeiro);

        consulta.Inicio.Should().Be(Primeiro);
        consulta.Fim.Should().Be(Dez);
    }

    [Fact]
    public void Periodo_de_um_dia_so_vale_para_aquele_dia()
    {
        var consulta = new ConsultaDeVendas(De: Dez, Ate: Dez);

        consulta.Inicio.Should().Be(Dez);
        consulta.Fim.Should().Be(Dez);
    }

    [Fact]
    public void Com_uma_ponta_so_a_outra_fica_aberta()
    {
        new ConsultaDeVendas(De: Dez).Should().BeEquivalentTo(new { Inicio = (DateOnly?)Dez, Fim = (DateOnly?)null });
        new ConsultaDeVendas(Ate: Dez).Should().BeEquivalentTo(new { Inicio = (DateOnly?)null, Fim = (DateOnly?)Dez });
    }
}
