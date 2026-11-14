const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const decimal = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const inteiro = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 0 });

export function formatarMoeda(valor) {
    return moeda.format(valor);
}

export function formatarDecimal(valor) {
    return decimal.format(valor);
}

export function formatarContagem(quantidade, singular, plural) {
    return `${inteiro.format(quantidade)} ${quantidade === 1 ? singular : plural}`;
}

const comVirgula = /^([1-9]\d{0,2}(\.\d{3})+|\d+),\d+$/;
const comMilhar = /^[1-9]\d{0,2}(\.\d{3})+$/;
const comPontoDecimal = /^\d+\.\d{1,2}$/;
const soDigitos = /^\d+$/;
const limiteDoValor = 1e16;

function normalizarDecimal(numero) {
    if (comVirgula.test(numero)) {
        return numero.replace(/\./g, '').replace(',', '.');
    }

    if (comMilhar.test(numero)) {
        return numero.replace(/\./g, '');
    }

    return comPontoDecimal.test(numero) || soDigitos.test(numero) ? numero : null;
}

function decompor(texto) {
    const limpo = String(texto ?? '').replace(/\s/g, '');
    const negativo = limpo.startsWith('-');
    return { numero: normalizarDecimal(negativo ? limpo.slice(1) : limpo), negativo };
}

export function escritaDecimalValida(texto) {
    return decompor(texto).numero !== null;
}

function separarDecimal(texto) {
    const { numero, negativo } = decompor(texto);
    return numero === null || Number(numero) >= limiteDoValor ? null : { numero, negativo };
}

export function reescreverDecimal(texto) {
    const separado = separarDecimal(texto);
    const casas = separado?.numero.split('.')[1]?.length ?? 0;
    return separado === null || casas > 2 ? null : formatarDecimalDeCentavos(lerCentavos(texto));
}

export function lerDecimal(texto) {
    const separado = separarDecimal(texto);
    if (separado === null) {
        return null;
    }

    const valor = Number(separado.numero);
    return separado.negativo ? -valor : valor;
}

export function lerCentavos(texto) {
    const separado = separarDecimal(texto);
    if (separado === null) {
        return null;
    }

    const [inteiro, fracao = ''] = separado.numero.split('.');
    const arredondamento = fracao.length > 2 && fracao[2] >= '5' ? 1n : 0n;
    const centavos = BigInt(inteiro) * 100n + BigInt(fracao.padEnd(2, '0').slice(0, 2)) + arredondamento;
    return separado.negativo ? -centavos : centavos;
}

function textoDosCentavos(centavos) {
    const absoluto = (centavos < 0n ? -centavos : centavos).toString().padStart(3, '0');
    return `${centavos < 0n ? '-' : ''}${absoluto.slice(0, -2)}.${absoluto.slice(-2)}`;
}

export function formatarMoedaDeCentavos(centavos) {
    return moeda.format(textoDosCentavos(centavos));
}

export function formatarDecimalDeCentavos(centavos) {
    return decimal.format(textoDosCentavos(centavos));
}

export function lerInteiro(texto) {
    const limpo = String(texto ?? '').trim();
    return /^\d+$/.test(limpo) && Number.isFinite(Number(limpo)) ? Number(limpo) : null;
}

export function iniciais(nome) {
    const partes = String(nome ?? '').split(' ').filter(Boolean);
    if (partes.length === 0) {
        return '';
    }

    const letras = partes.length === 1 ? partes[0][0] : partes[0][0] + partes.at(-1)[0];
    return letras.toLocaleUpperCase('pt-BR');
}

export function tomDoAvatar(nome) {
    return [...String(nome ?? '')].reduce((soma, letra) => (soma * 31 + letra.charCodeAt(0)) % 9973, 0) % 5;
}
