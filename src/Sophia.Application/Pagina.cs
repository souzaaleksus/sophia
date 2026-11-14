namespace Sophia.Application;

public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Numero, int Tamanho, int TotalDeItens)
{
    public int TotalDePaginas => Math.Max(1, (TotalDeItens + Tamanho - 1) / Tamanho);

    public bool TemAnterior => Numero > 1;

    public bool TemProxima => Numero < TotalDePaginas;

    public int PrimeiroItem => Itens.Count == 0 ? 0 : ((Numero - 1) * Tamanho) + 1;

    public int UltimoItem => Itens.Count == 0 ? 0 : PrimeiroItem + Itens.Count - 1;
}
