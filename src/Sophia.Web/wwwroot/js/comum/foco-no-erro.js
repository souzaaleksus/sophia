import { ligarMensagem, limparMensagem } from './erros.js';

const campoNomeado = erro => {
    const nome = erro.dataset.valmsgFor;
    return nome ? document.querySelector(`[name="${CSS.escape(nome)}"]:not([type=hidden])`) : null;
};

const campoDoErro = erro =>
    campoNomeado(erro) ?? erro.closest('.campo, section')?.querySelector('input:not([type=hidden]), select, textarea');

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
    campoNomeado(erro)?.addEventListener('input', () => limparMensagem(erro), { once: true });
    primeiro ??= campo;
});

primeiro?.focus();
