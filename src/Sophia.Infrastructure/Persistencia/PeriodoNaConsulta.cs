using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia;

internal static class PeriodoNaConsulta
{
    public static IQueryable<Venda> DoPeriodo(this IQueryable<Venda> vendas, DateOnly? inicio, DateOnly? fim)
    {
        if (inicio is { } de)
        {
            vendas = vendas.Where(v => v.Data >= de);
        }

        return fim is { } ate ? vendas.Where(v => v.Data <= ate) : vendas;
    }
}
