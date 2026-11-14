using Sophia.Application.Abstracoes;
using Sophia.Application.Vendas;

namespace Sophia.Application.Painel;

public sealed class PainelServico(
    IVendaRepositorio vendas,
    IRelogio relogio,
    ParametrosDoSophia parametros) : IPainelServico
{
    public async Task<PainelDto> ObterAsync(CancellationToken cancelamento = default)
    {
        var hoje = relogio.Hoje;
        var primeiroDiaDoMes = new DateOnly(hoje.Year, hoje.Month, 1);

        var dia = await vendas.ResumirAsync(hoje, hoje, cancelamento);
        var mes = await vendas.ResumirAsync(primeiroDiaDoMes, hoje, cancelamento);
        var ultimas = await vendas.ListarAsync(parametros.UltimasVendasNoPainel, cancelamento);
        var ultimosDias = await UltimosDiasAsync(hoje, cancelamento);
        var maisVendidos = await vendas.MaisVendidosAsync(
            primeiroDiaDoMes,
            hoje,
            parametros.MaisVendidosNoPainel,
            cancelamento);

        return new PainelDto(hoje, dia, mes, ultimas, ultimosDias, maisVendidos);
    }

    private async Task<IReadOnlyList<TotalDoDia>> UltimosDiasAsync(DateOnly hoje, CancellationToken cancelamento)
    {
        var primeiroDia = hoje.AddDays(1 - parametros.DiasNoGrafico);
        var comVenda = (await vendas.TotaisPorDiaAsync(primeiroDia, hoje, cancelamento)).ToDictionary(t => t.Dia);

        return Enumerable.Range(0, parametros.DiasNoGrafico)
            .Select(primeiroDia.AddDays)
            .Select(data => comVenda.GetValueOrDefault(data) ?? new TotalDoDia(data, 0m, 0))
            .ToList();
    }
}
