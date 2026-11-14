import { reescreverDecimal } from '../formato.js';

document.addEventListener('focusout', evento => {
    const campo = evento.target;
    if (!(campo instanceof HTMLInputElement) || campo.inputMode !== 'decimal') {
        return;
    }

    const reescrito = reescreverDecimal(campo.value);
    if (reescrito !== null && reescrito !== campo.value) {
        campo.value = reescrito;
    }
});
