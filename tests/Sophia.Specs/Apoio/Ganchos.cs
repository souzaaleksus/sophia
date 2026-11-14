using Reqnroll;
using Sophia.Testes.Banco;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Sophia.Specs.Apoio;

[Binding]
public static class Ganchos
{
    internal static readonly BancoDeTestes Banco = new();

    [BeforeTestRun]
    public static Task CriarOBancoAsync() => Banco.InitializeAsync();

    [AfterTestRun]
    public static Task ApagarOBancoAsync() => Banco.DisposeAsync();

    [BeforeScenario]
    public static Task LimparOBancoAsync() => Banco.LimparAsync();
}
