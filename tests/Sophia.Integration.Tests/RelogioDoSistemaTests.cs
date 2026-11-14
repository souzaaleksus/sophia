using Microsoft.Extensions.Time.Testing;
using Sophia.Infrastructure;

namespace Sophia.Integration.Tests;

public sealed class RelogioDoSistemaTests
{
    [Fact]
    public void Usa_a_data_do_fuso_de_brasilia_e_nao_a_do_servidor()
    {
        var vinteDuasHorasEmBrasilia = new DateTimeOffset(2026, 11, 14, 1, 0, 0, TimeSpan.Zero);
        var tempo = new FakeTimeProvider(vinteDuasHorasEmBrasilia);

        var relogio = new RelogioDoSistema(tempo, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

        relogio.Hoje.Should().Be(new DateOnly(2026, 11, 13));
    }
}
