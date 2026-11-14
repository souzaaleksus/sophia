namespace Sophia.Web.Models;

public sealed record ColunaOrdenavel(
    string Rotulo,
    string Ordem,
    string OrdemAtual,
    IReadOnlyDictionary<string, string> Filtros,
    string? Classe = null)
{
    public bool Ativa => OrdemAtual.TrimStart('-') == Ordem;

    public bool Decrescente => Ativa && OrdemAtual.StartsWith('-');

    public string? Sentido => (Ativa, Decrescente) switch
    {
        (false, _) => null,
        (true, true) => "descending",
        (true, false) => "ascending",
    };

    public string Icone => (Ativa, Decrescente) switch
    {
        (false, _) => "#icone-ordem",
        (true, true) => "#icone-ordem-decrescente",
        (true, false) => "#icone-ordem-crescente",
    };

    public Dictionary<string, string> RotaDoClique()
    {
        var proxima = Ativa && !Decrescente ? $"-{Ordem}" : Ordem;
        var rota = new Dictionary<string, string>(Filtros) { ["ordem"] = proxima };
        rota.Remove("pagina");
        return rota;
    }
}
