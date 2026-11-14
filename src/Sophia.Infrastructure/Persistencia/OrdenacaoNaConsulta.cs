using System.Linq.Expressions;

namespace Sophia.Infrastructure.Persistencia;

internal static class OrdenacaoNaConsulta
{
    public static IOrderedQueryable<T> OrdenarPor<T, TChave>(
        this IQueryable<T> consulta,
        Expression<Func<T, TChave>> chave,
        bool decrescente) =>
        decrescente ? consulta.OrderByDescending(chave) : consulta.OrderBy(chave);

    public static IOrderedQueryable<T> DepoisPor<T, TChave>(
        this IOrderedQueryable<T> consulta,
        Expression<Func<T, TChave>> chave,
        bool decrescente) =>
        decrescente ? consulta.ThenByDescending(chave) : consulta.ThenBy(chave);

    public static IQueryable<T> Recortar<T>(this IQueryable<T> consulta, int numero, int tamanho) =>
        consulta.Skip((int)Math.Min(int.MaxValue, (numero - 1L) * tamanho)).Take(tamanho);
}
