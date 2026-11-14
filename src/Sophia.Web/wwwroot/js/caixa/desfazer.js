const tempoParaDesfazer = 6000;
const tempoParaConferirDeNovo = 1000;

export function criarDesfazer(aviso, aoDesfazer) {
    let removido = null;
    let temporizador;

    aviso.querySelector('[data-acao=desfazer-remocao]').addEventListener('click', desfazer);

    function oferecer(item, posicao) {
        removido = { item, posicao };
        aviso.hidden = false;
        clearTimeout(temporizador);
        temporizador = setTimeout(esconder, tempoParaDesfazer);
    }

    function esconder() {
        if (aviso.matches(':focus-within, :hover')) {
            temporizador = setTimeout(esconder, tempoParaConferirDeNovo);
            return;
        }

        aviso.hidden = true;
        removido = null;
    }

    function desfazer() {
        if (removido) {
            aoDesfazer(removido.item, removido.posicao);
            removido = null;
            aviso.hidden = true;
        }
    }

    return { oferecer };
}
