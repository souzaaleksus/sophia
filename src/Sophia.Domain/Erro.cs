namespace Sophia.Domain;

public sealed record Erro(string Campo, string Mensagem, int? Linha = null);
