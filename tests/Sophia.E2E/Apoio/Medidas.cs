using Microsoft.Playwright;

namespace Sophia.E2E.Apoio;

public static class Medidas
{
    private const string Descrever = """
        e => {
            const r = e.getBoundingClientRect();
            const classes = typeof e.className === 'string' && e.className.trim() !== ''
                ? '.' + e.className.trim().split(/\s+/).join('.')
                : '';
            const texto = (e.getAttribute('aria-label') || e.textContent || e.value || '').trim().replace(/\s+/g, ' ').slice(0, 30);
            return `${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}${classes} "${texto}" ${Math.round(r.width)}x${Math.round(r.height)} em x=${Math.round(r.left)}`;
        }
        """;

    private const string RolagemLateral = $$"""
        () => {
            const descrever = {{Descrever}};
            const raiz = document.documentElement;
            if (raiz.scrollWidth <= raiz.clientWidth) {
                return '';
            }

            const largura = raiz.clientWidth;
            const fora = [...document.body.querySelectorAll('*')]
                .filter(e => e.getBoundingClientRect().right > largura + 0.5)
                .slice(0, 8)
                .map(descrever);
            return `scrollWidth ${raiz.scrollWidth}px > clientWidth ${largura}px; passam da borda: ${fora.join('; ')}`;
        }
        """;

    private const string AlvosPequenos = $$"""
        ([seletor, minimo]) => {
            const descrever = {{Descrever}};
            return [...document.querySelectorAll(seletor)]
                .filter(e => {
                    const r = e.getBoundingClientRect();
                    return r.width > 1 && r.height > 1
                        && getComputedStyle(e).visibility !== 'hidden'
                        && e.closest('[hidden]') === null;
                })
                .filter(e => {
                    const r = e.getBoundingClientRect();
                    return r.width < minimo || r.height < minimo;
                })
                .map(descrever);
        }
        """;

    private const string NoCentro = $$"""
        ([e, aceitaDescendente]) => {
            const descrever = {{Descrever}};
            const r = e.getBoundingClientRect();
            const atingido = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
            if (atingido === e || (aceitaDescendente && atingido !== null && e.contains(atingido))) {
                return '';
            }

            return atingido === null ? 'nenhum elemento (o centro está fora da janela)' : descrever(atingido);
        }
        """;

    public static Task<string> RolagemLateralAsync(IPage pagina) =>
        pagina.EvaluateAsync<string>(RolagemLateral);

    public static Task<string[]> AlvosMenoresQueAsync(IPage pagina, string seletor, int minimo) =>
        pagina.EvaluateAsync<string[]>(AlvosPequenos, new object[] { seletor, minimo });

    public static async Task<string> QuemOcupaOCentroAsync(ILocator elemento, bool aceitaDescendente)
    {
        var alvo = await elemento.ElementHandleAsync();
        return await elemento.Page.EvaluateAsync<string>(NoCentro, new object[] { alvo, aceitaDescendente });
    }
}
