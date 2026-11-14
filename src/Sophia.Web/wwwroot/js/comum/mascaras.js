const mascaras = {
    cpf: {
        limite: 11,
        formatar: digitos => digitos
            .replace(/^(\d{3})(\d)/, '$1.$2')
            .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
            .replace(/\.(\d{3})(\d{1,2})$/, '.$1-$2'),
    },
    telefone: {
        limite: 11,
        normalizar: (_, digitos) => (digitos.length > 11 && digitos.startsWith('55') ? digitos.slice(2) : digitos),
        formatar: formatarTelefone,
    },
    data: {
        limite: 8,
        normalizar: (texto, digitos) => {
            const iso = /^(\d{4})-(\d{2})-(\d{2})$/.exec(texto.trim());
            return iso ? `${iso[3]}${iso[2]}${iso[1]}` : digitos;
        },
        formatar: digitos => digitos
            .replace(/^(\d{2})(\d)/, '$1/$2')
            .replace(/^(\d{2})\/(\d{2})(\d)/, '$1/$2/$3'),
    },
};

function formatarTelefone(numero) {
    if (numero.length <= 2) {
        return numero.length > 0 ? `(${numero}` : '';
    }

    const corte = numero.length === 11 ? 7 : 6;
    const meio = numero.slice(2, corte);
    const fim = numero.slice(corte);
    return `(${numero.slice(0, 2)}) ${meio}${fim ? `-${fim}` : ''}`;
}

const contarDigitos = texto => texto.replace(/\D/g, '').length;

function posicaoDepoisDosDigitos(texto, quantos) {
    let vistos = 0;
    for (let posicao = 0; posicao < texto.length && quantos > 0; posicao++) {
        if (/\d/.test(texto[posicao])) {
            vistos += 1;
        }

        if (vistos === quantos) {
            return posicao + 1;
        }
    }

    return quantos === 0 ? 0 : texto.length;
}

function apagarAntesDoSeparador(evento, digitos, antes, anterior) {
    const apagouSoOSeparador = evento?.inputType === 'deleteContentBackward' && digitos === anterior.digitos && antes > 0;
    return apagouSoOSeparador
        ? { digitos: digitos.slice(0, antes - 1) + digitos.slice(antes), antes: antes - 1 }
        : { digitos, antes };
}

function mascarar(mascara, campo, evento, anterior) {
    const texto = campo.value;
    const cursor = campo.selectionStart ?? texto.length;
    const normalizar = mascara.normalizar ?? ((_, digitos) => digitos);
    let { digitos, antes } = apagarAntesDoSeparador(
        evento,
        normalizar(texto, texto.replace(/\D/g, '')),
        contarDigitos(texto.slice(0, cursor)),
        anterior);

    if (digitos.length > mascara.limite) {
        if (evento?.inputType === 'insertText') {
            return { ...anterior, cursor: Math.max(0, cursor - (evento.data ?? '').length) };
        }

        digitos = digitos.slice(0, mascara.limite);
        antes = Math.min(antes, mascara.limite);
    }

    const valor = mascara.formatar(digitos);
    return { valor, digitos, cursor: posicaoDepoisDosDigitos(valor, antes) };
}

document.querySelectorAll('[data-mascara]').forEach(campo => {
    const mascara = mascaras[campo.dataset.mascara];
    let anterior = { valor: '', digitos: '', cursor: 0 };
    const aplicar = evento => {
        anterior = mascarar(mascara, campo, evento, anterior);
        campo.value = anterior.valor;
        if (document.activeElement === campo) {
            campo.setSelectionRange(anterior.cursor, anterior.cursor);
        }
    };
    campo.addEventListener('input', aplicar);
    aplicar();
});
