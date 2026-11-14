import { buscarJson, criarAutocompletar } from '../autocompletar.js';
import { formatarDecimal, formatarMoeda, lerDecimal, lerInteiro } from '../formato.js';

const camposDaLinha = ['ProdutoId', 'ProdutoNome', 'ValorUnitario', 'Quantidade'];

export function criarLinhas({ area, modelo, configuracao, textosDaBusca, movimentoReduzido, aoMudar, aoRemover }) {
    const todas = () => [...area.querySelectorAll('[data-item]')];
    const campoDe = (item, nome) => item.querySelector(`[data-campo=${nome}]`);

    function preparar(item) {
        criarAutocompletar(campoDe(item, 'ProdutoNome'), {
            buscar: termo => buscarJson(configuracao.enderecoBuscaDeProdutos, termo),
            textos: { ...textosDaBusca, vazio: configuracao.textoProdutoNaoEncontrado },
            descrever: produto => ({ principal: produto.nome, secundario: formatarMoeda(produto.valorUnitario) }),
            aoDigitar: () => {
                campoDe(item, 'ProdutoId').value = '';
                aoMudar();
            },
            aoEscolher: produto => escolherProduto(item, produto),
        });

        campoDe(item, 'ValorUnitario').addEventListener('input', aoMudar);
        campoDe(item, 'ValorUnitario').addEventListener('blur', () => arredondarValor(item));
        campoDe(item, 'Quantidade').addEventListener('input', aoMudar);
        campoDe(item, 'Quantidade').addEventListener('keydown', abrirProximaComEnter);
        item.querySelector('[data-acao=menos]').addEventListener('click', () => mudarQuantidade(item, -1));
        item.querySelector('[data-acao=mais]').addEventListener('click', () => mudarQuantidade(item, 1));
        item.querySelector('[data-acao=remover-item]').addEventListener('click', () => remover(item));
    }

    function arredondarValor(item) {
        const valor = lerDecimal(campoDe(item, 'ValorUnitario').value);
        if (valor !== null) {
            campoDe(item, 'ValorUnitario').value = formatarDecimal(valor);
        }
    }

    function abrirProximaComEnter(evento) {
        if (evento.key === 'Enter' && !evento.ctrlKey && !evento.metaKey) {
            evento.preventDefault();
            evento.stopPropagation();
            adicionar().focus();
        }
    }

    function adicionar() {
        const item = modelo.content.firstElementChild.cloneNode(true);
        area.append(item);
        destacarChegada(item);
        preparar(item);
        renumerar();
        aoMudar();
        return campoDe(item, 'ProdutoNome');
    }

    function destacarChegada(item) {
        if (!movimentoReduzido.matches) {
            item.classList.add('caixa-item-novo');
            item.addEventListener('animationend', () => item.classList.remove('caixa-item-novo'), { once: true });
        }
    }

    function focarLivre() {
        const livre = todas().find(estaEmBranco);
        (livre ? campoDe(livre, 'ProdutoNome') : adicionar()).focus();
    }

    function escolherProduto(item, produto) {
        const jaLancado = todas().find(outro => outro !== item && campoDe(outro, 'ProdutoId').value === String(produto.id));
        if (jaLancado) {
            somarNaLinhaExistente(item, jaLancado);
            return;
        }

        campoDe(item, 'ProdutoId').value = produto.id;
        campoDe(item, 'ProdutoNome').value = produto.nome;
        campoDe(item, 'ValorUnitario').value = formatarDecimal(produto.valorUnitario);
        if (!campoDe(item, 'Quantidade').value) {
            campoDe(item, 'Quantidade').value = 1;
        }

        selecionar(campoDe(item, 'Quantidade'));
        aoMudar();
    }

    function somarNaLinhaExistente(item, existente) {
        const quantidade = campoDe(existente, 'Quantidade');
        quantidade.value = (lerInteiro(quantidade.value) ?? 0) + 1;
        campoDe(item, 'ProdutoNome').value = '';
        selecionar(quantidade);
        aoMudar();
    }

    function selecionar(campo) {
        campo.focus();
        campo.select();
    }

    function mudarQuantidade(item, passo) {
        const campo = campoDe(item, 'Quantidade');
        campo.value = Math.max(1, (lerInteiro(campo.value) ?? 0) + passo);
        aoMudar();
    }

    function remover(item) {
        const posicao = todas().indexOf(item);
        const proximo = item.nextElementSibling ?? item.previousElementSibling;
        item.remove();
        if (!estaEmBranco(item)) {
            aoRemover(item, posicao);
        }

        const foco = proximo ? campoDe(proximo, 'ProdutoNome') : adicionar();
        renumerar();
        aoMudar();
        foco.focus();
    }

    function reinserir(item, posicao) {
        area.insertBefore(item, todas()[posicao] ?? null);
        renumerar();
        aoMudar();
        campoDe(item, 'Quantidade').focus();
    }

    function estaEmBranco(item) {
        return camposDaLinha.every(nome => campoDe(item, nome).value.trim() === '');
    }

    function descartarEmBranco() {
        todas().filter(estaEmBranco).forEach(item => item.remove());
    }

    function temProdutoLancado() {
        return todas().some(item => campoDe(item, 'ProdutoId').value !== '');
    }

    function renumerar() {
        todas().forEach(renumerarLinha);
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

    todas().forEach(preparar);
    renumerar();

    return { todas, adicionar, focarLivre, reinserir, descartarEmBranco, temProdutoLancado, renumerar };
}
