namespace Sophia.Application.Vendas;

public sealed record ConsultaDeVendas(int Pagina = 1, DateOnly? De = null, DateOnly? Ate = null, string? Ordem = null)
{
    public DateOnly? Inicio => De > Ate ? Ate : De;

    public DateOnly? Fim => De > Ate ? De : Ate;
}
