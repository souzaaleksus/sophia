using System.ComponentModel.DataAnnotations;

namespace Sophia.Application;

public sealed class ParametrosDoSophia
{
    [Range(1, 200)]
    public int TamanhoDaPagina { get; init; } = 25;

    [Range(1, 31)]
    public int DiasNoGrafico { get; init; } = 7;

    [Range(1, 50)]
    public int MaisVendidosNoPainel { get; init; } = 5;

    [Range(1, 50)]
    public int UltimasVendasNoPainel { get; init; } = 5;

    [Range(1, 50)]
    public int SugestoesNaBusca { get; init; } = 8;
}
