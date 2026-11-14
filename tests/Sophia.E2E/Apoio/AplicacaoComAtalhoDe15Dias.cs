namespace Sophia.E2E.Apoio;

public sealed class AplicacaoComAtalhoDe15Dias()
    : AplicacaoEmExecucao(new Dictionary<string, string> { ["Sophia__DiasDoAtalhoDePeriodo"] = "15" });

[CollectionDefinition(Nome)]
public sealed class ColecaoDaAplicacaoComAtalhoDe15Dias : ICollectionFixture<AplicacaoComAtalhoDe15Dias>
{
    public const string Nome = "Aplicação com atalho de 15 dias";
}
