using System.Reflection;

namespace Sophia.Tests.Fakes;

internal sealed class Tabela<T>
    where T : class
{
    private static readonly MethodInfo CopiaRasa =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly PropertyInfo PropriedadeId = typeof(T).GetProperty("Id")!;

    private readonly List<T> _novos = [];
    private readonly List<T> _rastreados = [];
    private readonly List<T> _removidos = [];
    private List<T> _gravados = [];
    private int _ultimoId;

    public IReadOnlyList<T> Gravados => _gravados;

    public T? Obter(int id)
    {
        var gravado = _gravados.FirstOrDefault(e => Id(e) == id);
        if (gravado is null)
        {
            return null;
        }

        _rastreados.Clear();
        var copia = (T)CopiaRasa.Invoke(gravado, null)!;
        _rastreados.Add(copia);
        return copia;
    }

    public void Adicionar(T entidade) => _novos.Add(entidade);

    public void Remover(T entidade) => _removidos.Add(entidade);

    public IReadOnlyList<T> EstadoAposSalvar() =>
        _gravados
            .Where(g => _removidos.TrueForAll(r => Id(r) != Id(g)))
            .Select(g => _rastreados.LastOrDefault(r => Id(r) == Id(g)) ?? g)
            .Concat(_novos)
            .ToList();

    public IReadOnlyList<T> Removidos() => _removidos;

    public void Confirmar(Action<T, int>? aoGerarId = null)
    {
        var estado = EstadoAposSalvar().ToList();
        foreach (var novo in _novos)
        {
            var id = ++_ultimoId;
            PropriedadeId.SetValue(novo, id);
            aoGerarId?.Invoke(novo, id);
        }

        _gravados = estado;
        Descartar();
    }

    public void Descartar()
    {
        _novos.Clear();
        _rastreados.Clear();
        _removidos.Clear();
    }

    private static int Id(T entidade) => (int)PropriedadeId.GetValue(entidade)!;
}
