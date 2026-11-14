using Sophia.Testes.Banco;

namespace Sophia.Application.Tests.Apoio;

[CollectionDefinition(Nome)]
public sealed class ColecaoDoBanco : ICollectionFixture<BancoDeTestes>
{
    public const string Nome = "Banco SQL Server";
}
