using Sophia.Application.Abstracoes;

namespace Sophia.Infrastructure;

public sealed class RelogioDoSistema(TimeProvider tempo, TimeZoneInfo fuso) : IRelogio
{
    public DateOnly Hoje => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(tempo.GetUtcNow(), fuso).DateTime);
}
