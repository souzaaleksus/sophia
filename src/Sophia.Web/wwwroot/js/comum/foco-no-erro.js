import { ligarMensagem } from './erros.js';

const campoDoErro = erro => {
    const nome = erro.dataset.valmsgFor;
    const direto = nome ? document.querySelector(`[name="${CSS.escape(nome)}"]:not([type=hidden])`) : null;
    return direto ?? erro.closest('.campo, section')?.querySelector('input:not([type=hidden]), select, textarea');
};

let primeiro = null;
document.querySelectorAll('[data-valmsg-for]').forEach((erro, posicao) => {
    if (erro.textContent.trim() === '') {
        return;
    }

    const campo = campoDoErro(erro);
    if (!campo || campo.closest('[hidden]')) {
        return;
    }

    erro.id ||= `erro-${posicao}`;
    ligarMensagem(campo, erro);
    primeiro ??= campo;
});

primeiro?.focus();
