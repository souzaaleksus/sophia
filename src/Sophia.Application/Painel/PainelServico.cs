using Sophia.Application.Abstracoes;
using Sophia.Application.Vendas;

namespace Sophia.Application.Painel;

public sealed class PainelServico(IVendaRepositorio vendas, IRelogio relogio) : IPainelServico
{
    public const int QuantidadeDeUltimasVendas = 5;

    public async Task<PainelDto> ObterAsync(CancellationToken cancelamento = default)
    {
        var hoje = relogio.Hoje;
        var primeiroDiaDoMes = new DateOnly(hoje.Year, hoje.Month, 1);

        var dia = await vendas.ResumirAsync(hoje, hoje, cancelamento);
        var mes = await vendas.ResumirAsync(primeiroDiaDoMes, hoje, cancelamento);
        var ultimas = await vendas.ListarAsync(QuantidadeDeUltimasVendas, cancelamento);

        return new PainelDto(hoje, dia, mes, ultimas);
    }
}
