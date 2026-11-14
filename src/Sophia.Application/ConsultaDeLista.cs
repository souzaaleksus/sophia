namespace Sophia.Application;

public sealed record ConsultaDeLista(int Pagina = 1, string? Busca = null, string? Ordem = null);
