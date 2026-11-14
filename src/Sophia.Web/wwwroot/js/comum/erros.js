export function limparErro(raiz, nome) {
    raiz.querySelectorAll(`[data-valmsg-for="${CSS.escape(nome)}"]`).forEach(mensagem => {
        mensagem.textContent = '';
        mensagem.classList.remove('field-validation-error');
        if (mensagem.id) {
            document.querySelectorAll(`[aria-describedby~="${CSS.escape(mensagem.id)}"]`).forEach(campo => desligar(campo, mensagem.id));
        }
    });
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
