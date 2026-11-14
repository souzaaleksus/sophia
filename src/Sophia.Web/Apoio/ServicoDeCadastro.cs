using Sophia.Application;
using Sophia.Domain;

namespace Sophia.Web.Apoio;

public sealed record ServicoDeCadastro<TDto, TDados>(
    Func<ConsultaDeLista, CancellationToken, Task<Lista<TDto>>> Listar,
    Func<string?, int, CancellationToken, Task<IReadOnlyList<TDto>>> Buscar,
    Func<int, CancellationToken, Task<TDto?>> Obter,
    Func<TDados, CancellationToken, Task<Resultado<int>>> Cadastrar,
    Func<int, TDados, CancellationToken, Task<Resultado>> Alterar,
    Func<int, CancellationToken, Task<Resultado>> Excluir)
    where TDto : class;
