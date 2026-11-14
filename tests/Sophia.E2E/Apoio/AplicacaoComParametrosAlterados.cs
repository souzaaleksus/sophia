namespace Sophia.E2E.Apoio;

public sealed class AplicacaoComParametrosAlterados()
    : AplicacaoEmExecucao(new Dictionary<string, string>
    {
        ["Sophia__DiasDoAtalhoDePeriodo"] = "15",
        ["Sophia__DiasNoGrafico"] = "5",
    });

[CollectionDefinition(Nome)]
public sealed class ColecaoDaAplicacaoComParametrosAlterados : ICollectionFixture<AplicacaoComParametrosAlterados>
{
    public const string Nome = "Aplicação com parâmetros alterados";
}
