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

export function lerDecimal(texto) {
    const limpo = String(texto ?? '').trim().replace(/\s/g, '').replace(/\./g, '').replace(',', '.');
    if (limpo === '' || !/^-?\d+(\.\d+)?$/.test(limpo)) {
        return null;
    }

    return Number(limpo);
}

export function lerInteiro(texto) {
    const limpo = String(texto ?? '').trim();
    return /^\d+$/.test(limpo) ? Number(limpo) : null;
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

export function emCentavos(valor) {
    return Math.round(valor * 100);
}
