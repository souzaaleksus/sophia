import { deixarDeDescrever, descrever } from './erros.js';
import { formatarMoeda, lerDecimal, lidoComoMilhar, reescreverDecimal } from '../formato.js';

let notasMostradas = 0;

const campoDecimal = alvo => alvo instanceof HTMLInputElement && alvo.inputMode === 'decimal';

const notaDoMilhar = campo => campo.closest('.campo')?.querySelector('[data-nota-do-milhar]');

export function apagarNotaDoMilhar(campo) {
    const nota = notaDoMilhar(campo);
    if (nota?.id) {
        nota.textContent = '';
        deixarDeDescrever(campo, nota.id);
    }
}

function anotarMilhar(campo) {
    const nota = notaDoMilhar(campo);
    if (!nota || !lidoComoMilhar(campo.value)) {
        return;
    }

    notasMostradas += 1;
    nota.id ||= `nota-do-milhar-${notasMostradas}`;
    nota.textContent = nota.dataset.textoLidoComoMilhar.replace('{valor}', () => formatarMoeda(lerDecimal(campo.value)));
    descrever(campo, nota.id);
}

document.addEventListener('focusout', evento => {
    const campo = evento.target;
    if (!campoDecimal(campo)) {
        return;
    }

    anotarMilhar(campo);
    const reescrito = reescreverDecimal(campo.value);
    if (reescrito !== null && reescrito !== campo.value) {
        campo.value = reescrito;
    }
});

document.addEventListener('input', evento => {
    if (campoDecimal(evento.target)) {
        apagarNotaDoMilhar(evento.target);
    }
});
