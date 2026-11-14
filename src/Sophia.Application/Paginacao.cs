namespace Sophia.Application;

internal static class Paginacao
{
    public static async Task<Pagina<T>> RecortarAsync<T>(int pedida, Func<int, Task<Pagina<T>>> buscar)
    {
        var encontrada = await buscar(Math.Max(1, pedida));
        return encontrada.Numero > encontrada.TotalDePaginas
            ? await buscar(encontrada.TotalDePaginas)
            : encontrada;
    }
}
