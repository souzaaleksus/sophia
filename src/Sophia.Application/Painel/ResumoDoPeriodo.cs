namespace Sophia.Application.Painel;

public sealed record ResumoDoPeriodo(int QuantidadeDeVendas, decimal Total)
{
    public static ResumoDoPeriodo Vazio { get; } = new(0, 0m);

    public decimal TicketMedio => QuantidadeDeVendas == 0
        ? 0m
        : decimal.Round(Total / QuantidadeDeVendas, 2, MidpointRounding.AwayFromZero);
}
