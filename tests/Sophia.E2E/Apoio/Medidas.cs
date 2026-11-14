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

    private const string AlturasPequenas = $$"""
        ([seletor, minimo]) => {
            const descrever = {{Descrever}};
            return [...document.querySelectorAll(seletor)]
                .filter(e => {
                    const r = e.getBoundingClientRect();
                    return r.width > 1 && r.height > 1
                        && getComputedStyle(e).visibility !== 'hidden'
                        && e.closest('[hidden]') === null;
                })
                .filter(e => e.getBoundingClientRect().height < minimo)
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

    private const string Transbordando = $$"""
        seletor => {
            const descrever = {{Descrever}};
            return [...document.querySelectorAll(seletor)]
                .filter(e => e.getBoundingClientRect().width > 1 && e.closest('[hidden]') === null)
                .flatMap(e => {
                    const caixa = e.getBoundingClientRect();
                    const moldura = e.parentElement.closest('.cartao, .visor, .caixa-fechamento, .caixa-item')?.getBoundingClientRect();
                    const problemas = [];
                    if (e.scrollWidth > e.clientWidth + 1) {
                        problemas.push(`${descrever(e)} tem ${e.scrollWidth - e.clientWidth} px de conteúdo cortado`);
                    }

                    if (moldura && (caixa.left < moldura.left - 0.5 || caixa.right > moldura.right + 0.5)) {
                        problemas.push(`${descrever(e)} sai do cartão (${Math.round(moldura.left)} a ${Math.round(moldura.right)})`);
                    }

                    return problemas;
                });
        }
        """;

    private const string Sobrepostos = $$"""
        seletor => {
            const descrever = {{Descrever}};
            const cruzam = (a, b) => a.left < b.right - 0.5 && b.left < a.right - 0.5 && a.top < b.bottom - 0.5 && b.top < a.bottom - 0.5;
            return [...document.querySelectorAll(seletor)].flatMap(grupo => {
                const partes = [...grupo.children].filter(e => {
                    const r = e.getBoundingClientRect();
                    return r.width > 1 && r.height > 1 && getComputedStyle(e).position !== 'absolute';
                });
                return partes.flatMap((a, i) => partes.slice(i + 1)
                    .filter(b => cruzam(a.getBoundingClientRect(), b.getBoundingClientRect()))
                    .map(b => `${descrever(a)} cobre ${descrever(b)}`));
            });
        }
        """;

    public static Task<string[]> RoladoresLateraisAsync(IPage pagina) =>
        pagina.EvaluateAsync<string[]>("""
            () => [...document.querySelectorAll('*')]
                .filter(e => /auto|scroll/.test(getComputedStyle(e).overflowX) && e.scrollWidth > e.clientWidth + 1)
                .map(e => `${e.tagName.toLowerCase()}.${e.className} rola ${e.scrollWidth - e.clientWidth} px para o lado`)
            """);

    public static Task<string> RolagemLateralAsync(IPage pagina) =>
        pagina.EvaluateAsync<string>(RolagemLateral);

    public static Task<string[]> TransbordamentosAsync(IPage pagina, string seletor) =>
        pagina.EvaluateAsync<string[]>(Transbordando, seletor);

    public static Task<string[]> SobreposicoesAsync(IPage pagina, string seletorDoGrupo) =>
        pagina.EvaluateAsync<string[]>(Sobrepostos, seletorDoGrupo);

    public static Task<string[]> AlvosMenoresQueAsync(IPage pagina, string seletor, int minimo) =>
        pagina.EvaluateAsync<string[]>(AlvosPequenos, new object[] { seletor, minimo });

    public static Task<string[]> AlturasMenoresQueAsync(IPage pagina, string seletor, int minimo) =>
        pagina.EvaluateAsync<string[]>(AlturasPequenas, new object[] { seletor, minimo });

    public static async Task<string> QuemOcupaOCentroAsync(ILocator elemento, bool aceitaDescendente)
    {
        var alvo = await elemento.ElementHandleAsync();
        return await elemento.Page.EvaluateAsync<string>(NoCentro, new object[] { alvo, aceitaDescendente });
    }
}
