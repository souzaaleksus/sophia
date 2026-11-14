using Sophia.Application.Painel;

namespace Sophia.Application.Tests.Painel;

public sealed class ResumoDoPeriodoTests
{
    [Fact]
    public void Ticket_medio_e_o_total_dividido_pelo_numero_de_vendas()
    {
        new ResumoDoPeriodo(4, 100m).TicketMedio.Should().Be(25m);
    }

    [Fact]
    public void Ticket_medio_arredonda_para_centavos()
    {
        new ResumoDoPeriodo(3, 81.70m).TicketMedio.Should().Be(27.23m);
        new ResumoDoPeriodo(2, 0.05m).TicketMedio.Should().Be(0.03m);
    }

    [Fact]
    public void Sem_vendas_o_ticket_medio_e_zero()
    {
        ResumoDoPeriodo.Vazio.TicketMedio.Should().Be(0m);
    }
}
