using Sophia.Application.Vendas;

namespace Sophia.Application.Painel;

public sealed record PainelDto(
    DateOnly Hoje,
    ResumoDoPeriodo Dia,
    ResumoDoPeriodo Mes,
    IReadOnlyList<VendaResumoDto> UltimasVendas,
    IReadOnlyList<TotalDoDia> UltimosDias,
    IReadOnlyList<ProdutoMaisVendido> MaisVendidos);
