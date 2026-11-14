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

export function emCentavos(valor) {
    return Math.round(valor * 100);
}
