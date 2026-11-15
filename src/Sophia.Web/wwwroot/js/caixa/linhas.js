import { buscarJson, criarAutocompletar } from '../autocompletar.js';
import { apagarNotaDoMilhar } from '../comum/decimais.js';
import { limparErro, mostrarErro } from '../comum/erros.js';
import { escritaDecimalValida, formatarDecimal, formatarMoeda, lerDecimal, lerInteiro, temMaisDeDuasCasas } from '../formato.js';

const camposDaLinha = ['ProdutoId', 'ProdutoNome', 'ValorUnitario', 'Quantidade'];

export function criarLinhas(opcoes) {
    return new Linhas(opcoes);
}

const campoDe = (item, nome) => item.querySelector(`[data-campo=${nome}]`);

const estaEmBranco = item => camposDaLinha.every(nome => campoDe(item, nome).value.trim() === '');

const limparAvisos = (item, nome) => {
    limparErro(item, campoDe(item, nome).name);
    apagarNotaDoMilhar(campoDe(item, nome));
};

class Linhas {
    #area;
    #modelo;
    #configuracao;
    #textosDaBusca;
    #movimentoReduzido;
    #aoMudar;
    #aoRemover;
    #aoSomar;

    constructor({ area, modelo, configuracao, textosDaBusca, movimentoReduzido, aoMudar, aoRemover, aoSomar }) {
        this.#area = area;
        this.#modelo = modelo;
        this.#configuracao = configuracao;
        this.#textosDaBusca = textosDaBusca;
        this.#movimentoReduzido = movimentoReduzido;
        this.#aoMudar = aoMudar;
        this.#aoRemover = aoRemover;
        this.#aoSomar = aoSomar;
        this.todas().forEach(item => this.#preparar(item));
        this.renumerar();
    }

    todas() {
        return [...this.#area.querySelectorAll('[data-item]')];
    }

    adicionar() {
        const item = this.#modelo.content.firstElementChild.cloneNode(true);
        this.#area.append(item);
        this.#destacar(item);
        this.#preparar(item);
        this.renumerar();
        this.#aoMudar();
        return campoDe(item, 'ProdutoNome');
    }

    focarLivre() {
        const livre = this.todas().find(estaEmBranco);
        (livre ? campoDe(livre, 'ProdutoNome') : this.adicionar()).focus();
    }

    reinserir(item, posicao) {
        this.#area.insertBefore(item, this.todas()[posicao] ?? null);
        this.renumerar();
        this.#aoMudar();
        campoDe(item, 'Quantidade').focus();
    }

    descartarEmBranco() {
        this.todas().filter(estaEmBranco).forEach(item => item.remove());
    }

    temProdutoLancado() {
        return this.todas().some(item => campoDe(item, 'ProdutoId').value !== '');
    }

    renumerar() {
        this.todas().forEach(renumerarLinha);
    }

    #preparar(item) {
        criarAutocompletar(campoDe(item, 'ProdutoNome'), {
            buscar: termo => buscarJson(this.#configuracao.enderecoBuscaDeProdutos, termo),
            textos: { ...this.#textosDaBusca, vazio: this.#configuracao.textoProdutoNaoEncontrado },
            descrever: produto => ({ principal: produto.nome, secundario: formatarMoeda(produto.valorUnitario) }),
            aoDigitar: () => {
                campoDe(item, 'ProdutoId').value = '';
                this.#aoMudar();
            },
            aoEscolher: produto => this.#escolherProduto(item, produto),
        });

        campoDe(item, 'ValorUnitario').addEventListener('input', () => this.#corrigido(item, ['ValorUnitario']));
        campoDe(item, 'ValorUnitario').addEventListener('blur', () => this.#conferirValor(item));
        campoDe(item, 'Quantidade').addEventListener('input', () => this.#corrigido(item, ['Quantidade']));
        campoDe(item, 'Quantidade').addEventListener('blur', () => this.#conferirQuantidade(item));
        campoDe(item, 'Quantidade').addEventListener('keydown', evento => this.#abrirProximaComEnter(evento));
        item.querySelectorAll('[data-acao=menos], [data-acao=mais]').forEach(botao => botao.addEventListener('click', evento =>
            this.#mudarQuantidade(item, evento.currentTarget, evento.detail === 0)));
        item.querySelector('[data-acao=remover-item]').addEventListener('click', () => this.#remover(item));
    }

    #abrirProximaComEnter(evento) {
        if (evento.key === 'Enter' && !evento.ctrlKey && !evento.metaKey) {
            evento.preventDefault();
            evento.stopPropagation();
            this.adicionar().focus();
        }
    }

    #destacar(item) {
        if (this.#movimentoReduzido.matches) {
            return;
        }

        item.classList.remove('caixa-item-novo');
        void item.offsetWidth;
        item.classList.add('caixa-item-novo');
        item.addEventListener('animationend', () => item.classList.remove('caixa-item-novo'), { once: true });
    }

    #escolherProduto(item, produto) {
        const jaLancado = this.todas().find(outro => outro !== item && campoDe(outro, 'ProdutoId').value === String(produto.id));
        if (jaLancado) {
            this.#somarNaLinhaExistente(item, jaLancado, produto);
            return;
        }

        campoDe(item, 'ProdutoId').value = produto.id;
        campoDe(item, 'ProdutoNome').value = produto.nome;
        campoDe(item, 'ValorUnitario').value = formatarDecimal(produto.valorUnitario);
        if (!campoDe(item, 'Quantidade').value) {
            campoDe(item, 'Quantidade').value = 1;
        }

        selecionar(campoDe(item, 'Quantidade'));
        this.#corrigido(item, camposDaLinha);
    }

    #conferirValor(item) {
        const campo = campoDe(item, 'ValorUnitario');
        const aviso = avisoDoValor(campo, this.#configuracao);
        if (aviso) {
            mostrarErro(campo, aviso);
        }
    }

    #conferirQuantidade(item) {
        const campo = campoDe(item, 'Quantidade');
        if ((lerInteiro(campo.value) ?? 0) > (lerInteiro(campo.max) ?? Infinity)) {
            mostrarErro(campo, this.#configuracao.textoQuantidadeAcimaDoMaximo);
        }
    }

    #corrigido(item, campos) {
        campos.forEach(nome => limparAvisos(item, nome));
        limparErro(this.#area.closest('form'), 'Itens');
        this.#aoMudar();
    }

    #somarNaLinhaExistente(item, existente, produto) {
        const quantidade = campoDe(existente, 'Quantidade');
        const atual = lerInteiro(quantidade.value) ?? 0;
        const somada = limitarQuantidade(quantidade, atual + 1);
        quantidade.value = somada;
        camposDaLinha.forEach(nome => {
            campoDe(item, nome).value = '';
            limparAvisos(item, nome);
        });
        selecionar(quantidade);
        this.#corrigido(existente, ['Quantidade']);
        this.#destacar(existente);
        this.#aoSomar(produto, this.todas().indexOf(existente) + 1, somada <= atual);
    }

    #mudarQuantidade(item, botao, peloTeclado) {
        const campo = campoDe(item, 'Quantidade');
        const passo = botao.dataset.acao === 'mais' ? 1 : -1;
        campo.value = limitarQuantidade(campo, (lerInteiro(campo.value) ?? 0) + passo);
        this.#corrigido(item, ['Quantidade']);
        if (peloTeclado && botao.disabled) {
            campo.focus();
        }
    }

    #remover(item) {
        const posicao = this.todas().indexOf(item);
        const proximo = item.nextElementSibling ?? item.previousElementSibling;
        item.remove();
        if (!estaEmBranco(item)) {
            this.#aoRemover(item, posicao);
        }

        const foco = proximo ? campoDe(proximo, 'ProdutoNome') : this.adicionar();
        this.renumerar();
        this.#aoMudar();
        foco.focus();
    }
}

function avisoDoValor(campo, configuracao) {
    const texto = campo.value;
    if (texto.trim() === '') {
        return null;
    }

    if (!escritaDecimalValida(texto)) {
        return configuracao.textoValorInvalido;
    }

    if (lerDecimal(texto) > (lerDecimal(campo.dataset.valorMaximo) ?? Infinity)) {
        return configuracao.textoValorAcimaDoPermitido;
    }

    return temMaisDeDuasCasas(texto) ? configuracao.textoValorComMaisDeDuasCasas : null;
}

function limitarQuantidade(campo, quantidade) {
    const maximo = lerInteiro(campo.max) ?? Number.MAX_SAFE_INTEGER;
    return Math.min(maximo, Math.max(1, quantidade));
}

function selecionar(campo) {
    campo.focus();
    campo.select();
}

function renumerarLinha(item, indice) {
    item.querySelectorAll('[data-campo]:not([data-campo=Subtotal])').forEach(elemento => {
        const nome = elemento.dataset.campo;
        elemento.name = `Itens[${indice}].${nome}`;
        if (elemento.type !== 'hidden') {
            elemento.id = `item-${indice}-${nome}`;
            elemento.closest('.campo').querySelector('label')?.setAttribute('for', elemento.id);
        }
    });

    const lista = item.querySelector('[role=listbox]');
    lista.id = `item-${indice}-opcoes`;
    campoDe(item, 'ProdutoNome').setAttribute('aria-controls', lista.id);
    item.querySelectorAll('[data-erro]').forEach(erro => {
        erro.dataset.valmsgFor = `Itens[${indice}].${erro.dataset.erro}`;
    });
}
