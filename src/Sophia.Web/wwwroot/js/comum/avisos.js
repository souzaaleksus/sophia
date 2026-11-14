const tempoNaTela = 6000;
const movimentoReduzido = window.matchMedia('(prefers-reduced-motion: reduce)');

const fechar = aviso => {
    if (aviso.contains(document.activeElement)) {
        document.getElementById('conteudo')?.focus();
    }

    if (movimentoReduzido.matches) {
        aviso.remove();
        return;
    }

    aviso.animate(
        [{ opacity: 1, transform: 'none' }, { opacity: 0, transform: 'translateY(-0.5rem)' }],
        { duration: 220, easing: 'ease-in' },
    ).finished.then(() => aviso.remove());
};

document.querySelectorAll('[data-aviso]').forEach(aviso => {
    aviso.querySelector('[data-acao=fechar-aviso]')?.addEventListener('click', () => fechar(aviso));
});

document.querySelectorAll('[data-aviso=temporario]').forEach(aviso => {
    const fecharQuandoLivre = () => {
        if (aviso.matches(':hover, :focus-within')) {
            window.setTimeout(fecharQuandoLivre, 1000);
            return;
        }

        fechar(aviso);
    };

    window.setTimeout(fecharQuandoLivre, tempoNaTela);
});
