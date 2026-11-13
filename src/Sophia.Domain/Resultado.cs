namespace Sophia.Domain;

public class Resultado
{
    private protected Resultado(IReadOnlyList<Erro> erros)
    {
        Erros = erros;
    }

    public IReadOnlyList<Erro> Erros { get; }

    public bool Sucesso => Erros.Count == 0;

    public static Resultado Ok() => new([]);

    public static Resultado<T> Ok<T>(T valor) => new(valor, []);

    public static Resultado Falha(params Erro[] erros) => new(ExigirAoMenosUm(erros));

    public static Resultado Falha(IEnumerable<Erro> erros) => new(ExigirAoMenosUm(erros));

    public static Resultado<T> Falha<T>(params Erro[] erros) => new(default, ExigirAoMenosUm(erros));

    public static Resultado<T> Falha<T>(IEnumerable<Erro> erros) => new(default, ExigirAoMenosUm(erros));

    private static List<Erro> ExigirAoMenosUm(IEnumerable<Erro> erros)
    {
        var lista = erros.ToList();
        if (lista.Count == 0)
        {
            throw new ArgumentException("Uma falha precisa de ao menos um erro.", nameof(erros));
        }

        return lista;
    }
}

public sealed class Resultado<T> : Resultado
{
    private readonly T? _valor;

    internal Resultado(T? valor, IReadOnlyList<Erro> erros)
        : base(erros)
    {
        _valor = valor;
    }

    public T Valor => Sucesso
        ? _valor!
        : throw new InvalidOperationException("Um resultado com erros não tem valor.");
}
