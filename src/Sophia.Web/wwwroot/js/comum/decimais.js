import { deixarDeDescrever, descrever } from './erros.js';
import { formatarMoeda, lerDecimal, lidoComoMilhar, reescreverDecimal } from '../formato.js';

let notas = 0;

const campoDecimal = alvo => alvo instanceof HTMLInputElement && alvo.inputMode === 'decimal';

export function atualizarNotaDoMilhar(campo) {
    const nota = (campo.closest('[data-item]') ?? campo.closest('.campo'))?.querySelector('[data-nota-do-milhar]');
    const texto = lidoComoMilhar(campo.value)
        ? nota?.dataset.textoLidoComoMilhar.replace('{valor}', () => formatarMoeda(lerDecimal(campo.value)))
        : '';
    if (!nota || nota.textContent === texto) {
        return;
    }

    notas += 1;
    nota.id ||= `nota-do-milhar-${notas}`;
    nota.textContent = texto;
    (texto ? descrever : deixarDeDescrever)(campo, nota.id);
}

document.addEventListener('input', evento => {
    if (campoDecimal(evento.target)) {
        atualizarNotaDoMilhar(evento.target);
    }
});

document.addEventListener('focusout', evento => {
    const campo = evento.target;
    if (!campoDecimal(campo)) {
        return;
    }

    const reescrito = reescreverDecimal(campo.value);
    if (reescrito !== null && reescrito !== campo.value) {
        campo.value = reescrito;
    }
});
