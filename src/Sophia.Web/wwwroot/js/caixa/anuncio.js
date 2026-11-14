const tempoNaTela = 6000;

export function criarAnuncio(regiao) {
    let temporizador;

    return texto => {
        regiao.textContent = texto;
        clearTimeout(temporizador);
        temporizador = setTimeout(() => {
            regiao.textContent = '';
        }, tempoNaTela);
    };
}
