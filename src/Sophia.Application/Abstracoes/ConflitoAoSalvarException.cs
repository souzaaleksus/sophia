namespace Sophia.Application.Abstracoes;

public sealed class ConflitoAoSalvarException : Exception
{
    public ConflitoAoSalvarException()
    {
    }

    public ConflitoAoSalvarException(string mensagem)
        : base(mensagem)
    {
    }

    public ConflitoAoSalvarException(string mensagem, Exception causa)
        : base(mensagem, causa)
    {
    }

    public ConflitoAoSalvarException(TipoDeConflito tipo, Exception? causa = null)
        : base($"O banco recusou a gravação: {tipo}.", causa)
    {
        Tipo = tipo;
    }

    public TipoDeConflito Tipo { get; }
}

public enum TipoDeConflito
{
    ValorDuplicado,
    RegistroEmUso,
}
