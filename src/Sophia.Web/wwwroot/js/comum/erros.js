let avisosMostrados = 0;

export function ligarMensagem(campo, mensagem) {
    const descricoes = new Set((campo.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean));
    descricoes.add(mensagem.id);
    campo.setAttribute('aria-describedby', [...descricoes].join(' '));
    campo.setAttribute('aria-invalid', 'true');
}

export function mostrarErro(campo, texto) {
    const mensagem = campo.form?.querySelector(`[data-valmsg-for="${CSS.escape(campo.name)}"]`);
    if (!mensagem) {
        return;
    }

    mensagem.textContent = texto;
    mensagem.classList.add('field-validation-error');
    avisosMostrados += 1;
    mensagem.id ||= `aviso-no-campo-${avisosMostrados}`;
    ligarMensagem(campo, mensagem);
}

export function limparMensagem(mensagem) {
    mensagem.textContent = '';
    mensagem.classList.remove('field-validation-error');
    if (mensagem.id) {
        document.querySelectorAll(`[aria-describedby~="${CSS.escape(mensagem.id)}"]`).forEach(campo => desligar(campo, mensagem.id));
    }
}

export function limparErro(raiz, nome) {
    raiz.querySelectorAll(`[data-valmsg-for="${CSS.escape(nome)}"]`).forEach(limparMensagem);
}

function desligar(campo, id) {
    const restantes = campo.getAttribute('aria-describedby').split(' ').filter(parte => parte !== '' && parte !== id);
    if (restantes.length > 0) {
        campo.setAttribute('aria-describedby', restantes.join(' '));
    } else {
        campo.removeAttribute('aria-describedby');
    }

    campo.removeAttribute('aria-invalid');
}
