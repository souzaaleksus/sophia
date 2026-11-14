import { criarAnuncio } from './caixa/anuncio.js';
import { ligarAtalhos } from './caixa/atalhos.js';
import { criarEscolhaDeCliente } from './caixa/cliente.js';
import { criarDesfazer } from './caixa/desfazer.js';
import { criarLinhas } from './caixa/linhas.js';
import { criarVisor, lerLinha, mostrarSubtotal, somarLinhas } from './caixa/totais.js';

const formulario = document.getElementById('nova-venda');
const configuracao = formulario.dataset;
const textosDaBusca = { buscando: configuracao.textoBuscando, falha: configuracao.textoFalhaNaBusca };
const movimentoReduzido = window.matchMedia('(prefers-reduced-motion: reduce)');
const visor = criarVisor(formulario, configuracao, movimentoReduzido);
const anunciar = criarAnuncio(formulario.querySelector('[data-anuncio]'));
const desfazer = criarDesfazer(formulario.querySelector('[data-desfazer]'), (item, posicao) => linhas.reinserir(item, posicao));
const linhas = criarLinhas({
    area: formulario.querySelector('[data-itens]'),
    modelo: document.getElementById('modelo-de-item'),
    configuracao,
    textosDaBusca,
    movimentoReduzido,
    aoMudar: () => recalcularTotais(),
    aoRemover: (item, posicao) => desfazer.oferecer(item, posicao),
    aoSomar: (produto, linha) => anunciar(configuracao.textoProdutoSomado.replace('{produto}', produto.nome).replace('{linha}', linha)),
});
const cliente = criarEscolhaDeCliente(formulario, configuracao, textosDaBusca, () => linhas.focarLivre());

recalcularTotais();

ligarAtalhos({
    buscarCliente: () => cliente.buscarOutro(),
    lancarProduto: () => linhas.focarLivre(),
    finalizar: () => formulario.requestSubmit(),
});

formulario.querySelector('[data-acao=adicionar-item]').addEventListener('click', () => linhas.adicionar().focus());

formulario.addEventListener('keydown', evento => {
    if (evento.key === 'Enter' && !evento.ctrlKey && evento.target.tagName === 'INPUT') {
        evento.preventDefault();
    }
});

formulario.addEventListener('submit', () => {
    if (formulario.dataset.enviando !== 'true') {
        linhas.descartarEmBranco();
        linhas.renumerar();
    }
});

window.addEventListener('pageshow', evento => {
    if (evento.persisted) {
        window.location.reload();
    }
});

window.addEventListener('beforeunload', evento => {
    if (formulario.dataset.enviando !== 'true' && linhas.temProdutoLancado()) {
        evento.preventDefault();
        evento.returnValue = '';
    }
});

formulario.dataset.pronto = 'true';

function recalcularTotais() {
    const itens = [...formulario.querySelectorAll('[data-itens] [data-item]')];
    const dados = itens.map(lerLinha);
    const soma = somarLinhas(dados);
    itens.forEach((item, posicao) => mostrarSubtotal(item, soma.subtotaisEmCentavos[posicao], dados[posicao].quantidade));
    visor.mostrar(soma);
}
