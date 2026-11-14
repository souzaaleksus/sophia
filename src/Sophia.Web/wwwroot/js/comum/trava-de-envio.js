const conteudoOriginal = new WeakMap();

const travar = (formulario, botao) => {
    formulario.dataset.enviando = 'true';
    if (!botao) {
        return;
    }

    conteudoOriginal.set(botao, [...botao.childNodes]);
    botao.disabled = true;
    botao.setAttribute('aria-busy', 'true');
    botao.textContent = formulario.dataset.textoEnviando;
};

const liberar = formulario => {
    delete formulario.dataset.enviando;
    formulario.querySelectorAll('[aria-busy=true]').forEach(botao => {
        botao.disabled = false;
        botao.removeAttribute('aria-busy');
        botao.replaceChildren(...(conteudoOriginal.get(botao) ?? botao.childNodes));
    });
};

window.addEventListener('submit', evento => {
    const formulario = evento.target;
    if (!(formulario instanceof HTMLFormElement) || !formulario.hasAttribute('data-envio')) {
        return;
    }

    if (formulario.dataset.enviando === 'true') {
        evento.preventDefault();
        return;
    }

    if (!evento.defaultPrevented) {
        travar(formulario, evento.submitter ?? formulario.querySelector('[type=submit]'));
    }
});

window.addEventListener('pageshow', evento => {
    if (evento.persisted) {
        document.querySelectorAll('form[data-envio]').forEach(liberar);
    }
});
