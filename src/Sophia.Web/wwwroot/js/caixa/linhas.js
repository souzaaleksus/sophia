import { buscarJson, criarAutocompletar } from '../autocompletar.js';
import { limparErro } from '../comum/erros.js';
import { formatarDecimal, formatarMoeda, lerDecimal, lerInteiro } from '../formato.js';

const camposDaLinha = ['ProdutoId', 'ProdutoNome', 'ValorUnitario', 'Quantidade'];

export function criarLinhas(opcoes) {
    return new Linhas(opcoes);
}

const campoDe = (item, nome) => item.querySelector(`[data-campo=${nome}]`);

const estaEmBranco = item => camposDaLinha.every(nome => campoDe(item, nome).value.trim() === '');

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
        campoDe(item, 'ValorUnitario').addEventListener('blur', () => arredondarValor(item));
        campoDe(item, 'Quantidade').addEventListener('input', () => this.#corrigido(item, ['Quantidade']));
        campoDe(item, 'Quantidade').addEventListener('keydown', evento => this.#abrirProximaComEnter(evento));
        item.querySelector('[data-acao=menos]').addEventListener('click', evento => this.#mudarQuantidade(item, -1, evento.detail === 0));
        item.querySelector('[data-acao=mais]').addEventListener('click', () => this.#mudarQuantidade(item, 1));
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

    #corrigido(item, campos) {
        campos.forEach(nome => limparErro(item, campoDe(item, nome).name));
        limparErro(this.#area.closest('form'), 'Itens');
        this.#aoMudar();
    }

    #somarNaLinhaExistente(item, existente, produto) {
        const quantidade = campoDe(existente, 'Quantidade');
        quantidade.value = limitarQuantidade(quantidade, (lerInteiro(quantidade.value) ?? 0) + 1);
        camposDaLinha.forEach(nome => {
            campoDe(item, nome).value = '';
            limparErro(item, campoDe(item, nome).name);
        });
        selecionar(quantidade);
        this.#corrigido(existente, ['Quantidade']);
        this.#destacar(existente);
        this.#aoSomar(produto, this.todas().indexOf(existente) + 1);
    }

    #mudarQuantidade(item, passo, peloTeclado = false) {
        const campo = campoDe(item, 'Quantidade');
        campo.value = limitarQuantidade(campo, (lerInteiro(campo.value) ?? 0) + passo);
        this.#corrigido(item, ['Quantidade']);
        if (peloTeclado && item.querySelector('[data-acao=menos]').disabled) {
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

function arredondarValor(item) {
    const valor = lerDecimal(campoDe(item, 'ValorUnitario').value);
    if (valor !== null) {
        campoDe(item, 'ValorUnitario').value = formatarDecimal(valor);
    }
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
