using Sophia.Application.Abstracoes;

namespace Sophia.Tests.Fakes;

public sealed class RelogioFixo(DateOnly hoje) : IRelogio
{
    public DateOnly Hoje { get; set; } = hoje;
}
