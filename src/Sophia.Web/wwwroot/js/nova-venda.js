import { buscarJson, criarAutocompletar } from './autocompletar.js';
import { emCentavos, formatarContagem, formatarDecimal, formatarMoeda, lerDecimal, lerInteiro } from './formato.js';

const formulario = document.getElementById('nova-venda');
const configuracao = formulario.dataset;
const textosDaBusca = {
    buscando: configuracao.textoBuscando,
    falha: configuracao.textoFalhaNaBusca,
};
const areaDosItens = formulario.querySelector('[data-itens]');
const modeloDeItem = document.getElementById('modelo-de-item');
const totalDaVenda = formulario.querySelector('[data-testid=total-da-venda]');
const valorNoVisor = totalDaVenda.querySelector('[data-visor=valor]');
const itensDaVenda = formulario.querySelector('[data-testid=itens-da-venda]');
const unidadesDaVenda = formulario.querySelector('[data-testid=unidades-da-venda]');
const movimentoReduzido = window.matchMedia('(prefers-reduced-motion: reduce)');
const camposDoItem = ['ProdutoId', 'ProdutoNome', 'ValorUnitario', 'Quantidade'];

const cliente = prepararCliente();
areaDosItens.querySelectorAll('[data-item]').forEach(prepararItem);
renumerarItens();
recalcularTotais();

formulario.querySelector('[data-acao=adicionar-item]').addEventListener('click', () => adicionarItem().focus());

formulario.addEventListener('keydown', evento => {
    if (evento.key === 'Enter' && !evento.ctrlKey && evento.target.tagName === 'INPUT') {
        evento.preventDefault();
    }
});

document.addEventListener('keydown', evento => {
    const atalho = atalhoDe(evento);
    if (atalho) {
        evento.preventDefault();
        atalho();
    }
});

formulario.addEventListener('submit', () => {
    if (formulario.dataset.enviando !== 'true') {
        descartarLinhasEmBranco();
        renumerarItens();
    }
});

window.addEventListener('beforeunload', evento => {
    if (formulario.dataset.enviando !== 'true' && temProdutoLancado()) {
        evento.preventDefault();
        evento.returnValue = '';
    }
});

function temProdutoLancado() {
    return [...areaDosItens.querySelectorAll('[data-campo=ProdutoId]')].some(campo => campo.value !== '');
}

function atalhoDe(evento) {
    if (evento.key === 'F2' && !evento.ctrlKey && !evento.altKey) {
        return () => cliente.buscarOutro();
    }

    if (evento.key === 'F4' && !evento.ctrlKey && !evento.altKey) {
        return focarProdutoLivre;
    }

    if (evento.key === 'Enter' && (evento.ctrlKey || evento.metaKey)) {
        return () => formulario.requestSubmit();
    }

    return null;
}

function prepararCliente() {
    const campo = nome => formulario.querySelector(`[data-cliente=${nome}]`);
    const busca = campo('busca');
    const escolhido = campo('escolhido');
    const entrada = busca.querySelector('input');

    criarAutocompletar(entrada, {
        buscar: termo => buscarJson(configuracao.enderecoBuscaDeClientes, termo),
        descrever: encontrado => ({ principal: encontrado.nome, secundario: encontrado.cpf }),
        textos: { ...textosDaBusca, vazio: configuracao.textoClienteNaoEncontrado },
        cadastro: { texto: configuracao.textoCadastrarCliente, endereco: configuracao.enderecoNovoCliente },
        aoEscolher: encontrado => {
            campo('id').value = encontrado.id;
            campo('nome').value = encontrado.nome;
            campo('cpf').value = encontrado.cpf;
            campo('nome-exibido').textContent = encontrado.nome;
            campo('cpf-exibido').textContent = encontrado.cpf;
            busca.hidden = true;
            escolhido.hidden = false;
            focarProdutoLivre();
        },
    });

    function buscarOutro() {
        if (!escolhido.hidden) {
            campo('id').value = '';
            campo('nome').value = '';
            campo('cpf').value = '';
            escolhido.hidden = true;
            busca.hidden = false;
            entrada.value = '';
        }

        entrada.focus();
        entrada.select();
    }

    campo('trocar').addEventListener('click', buscarOutro);

    return { buscarOutro };
}

function focarProdutoLivre() {
    const livre = [...areaDosItens.querySelectorAll('[data-item]')].find(estaEmBranco);
    const produto = livre ? livre.querySelector('[data-campo=ProdutoNome]') : adicionarItem();
    produto.focus();
}

function adicionarItem() {
    const item = modeloDeItem.content.firstElementChild.cloneNode(true);
    areaDosItens.append(item);
    if (!movimentoReduzido.matches) {
        item.classList.add('caixa-item-novo');
        item.addEventListener('animationend', () => item.classList.remove('caixa-item-novo'), { once: true });
    }

    prepararItem(item);
    renumerarItens();
    recalcularTotais();
    return item.querySelector('[data-campo=ProdutoNome]');
}

function prepararItem(item) {
    const campo = nome => item.querySelector(`[data-campo=${nome}]`);

    criarAutocompletar(campo('ProdutoNome'), {
        buscar: termo => buscarJson(configuracao.enderecoBuscaDeProdutos, termo),
        textos: { ...textosDaBusca, vazio: configuracao.textoProdutoNaoEncontrado },
        descrever: produto => ({ principal: produto.nome, secundario: formatarMoeda(produto.valorUnitario) }),
        aoDigitar: () => {
            campo('ProdutoId').value = '';
            recalcularTotais();
        },
        aoEscolher: produto => escolherProduto(item, produto),
    });

    campo('ValorUnitario').addEventListener('input', recalcularTotais);
    campo('ValorUnitario').addEventListener('blur', () => {
        const valor = lerDecimal(campo('ValorUnitario').value);
        if (valor !== null) {
            campo('ValorUnitario').value = formatarDecimal(valor);
        }
    });
    campo('Quantidade').addEventListener('input', recalcularTotais);
    campo('Quantidade').addEventListener('keydown', evento => {
        if (evento.key === 'Enter' && !evento.ctrlKey && !evento.metaKey) {
            evento.preventDefault();
            evento.stopPropagation();
            adicionarItem().focus();
        }
    });

    item.querySelector('[data-acao=menos]').addEventListener('click', () => mudarQuantidade(item, -1));
    item.querySelector('[data-acao=mais]').addEventListener('click', () => mudarQuantidade(item, 1));
    item.querySelector('[data-acao=remover-item]').addEventListener('click', () => removerItem(item));
}

function escolherProduto(item, produto) {
    const jaLancado = [...areaDosItens.querySelectorAll('[data-item]')]
        .find(outro => outro !== item && outro.querySelector('[data-campo=ProdutoId]').value === String(produto.id));

    if (jaLancado) {
        const quantidade = jaLancado.querySelector('[data-campo=Quantidade]');
        quantidade.value = (lerInteiro(quantidade.value) ?? 0) + 1;
        item.querySelector('[data-campo=ProdutoNome]').value = '';
        quantidade.focus();
        quantidade.select();
        recalcularTotais();
        return;
    }

    const campo = nome => item.querySelector(`[data-campo=${nome}]`);
    campo('ProdutoId').value = produto.id;
    campo('ProdutoNome').value = produto.nome;
    campo('ValorUnitario').value = formatarDecimal(produto.valorUnitario);
    if (!campo('Quantidade').value) {
        campo('Quantidade').value = 1;
    }

    campo('Quantidade').focus();
    campo('Quantidade').select();
    recalcularTotais();
}

function mudarQuantidade(item, passo) {
    const campo = item.querySelector('[data-campo=Quantidade]');
    const atual = lerInteiro(campo.value) ?? 0;
    campo.value = Math.max(1, atual + passo);
    recalcularTotais();
}

function removerItem(item) {
    const proximo = item.nextElementSibling ?? item.previousElementSibling;
    item.remove();
    const foco = proximo ? proximo.querySelector('[data-campo=ProdutoNome]') : adicionarItem();

    renumerarItens();
    recalcularTotais();
    foco.focus();
}

function estaEmBranco(item) {
    return camposDoItem.every(nome => item.querySelector(`[data-campo=${nome}]`).value.trim() === '');
}

function descartarLinhasEmBranco() {
    areaDosItens.querySelectorAll('[data-item]').forEach(item => {
        if (estaEmBranco(item)) {
            item.remove();
        }
    });
}

function renumerarItens() {
    areaDosItens.querySelectorAll('[data-item]').forEach((item, indice) => {
        item.querySelectorAll('[data-campo]').forEach(elemento => {
            const nome = elemento.dataset.campo;
            if (nome === 'Subtotal') {
                return;
            }

            elemento.name = `Itens[${indice}].${nome}`;
            if (elemento.type !== 'hidden') {
                elemento.id = `item-${indice}-${nome}`;
                elemento.closest('.campo').querySelector('label')?.setAttribute('for', elemento.id);
            }
        });

        const lista = item.querySelector('[role=listbox]');
        lista.id = `item-${indice}-opcoes`;
        item.querySelector('[data-campo=ProdutoNome]').setAttribute('aria-controls', lista.id);

        item.querySelectorAll('[data-erro]').forEach(erro => {
            erro.dataset.valmsgFor = `Itens[${indice}].${erro.dataset.erro}`;
        });
    });
}

function recalcularTotais() {
    let totalEmCentavos = 0;
    let itens = 0;
    let unidades = 0;

    areaDosItens.querySelectorAll('[data-item]').forEach(item => {
        const valor = lerDecimal(item.querySelector('[data-campo=ValorUnitario]').value) ?? 0;
        const quantidade = lerInteiro(item.querySelector('[data-campo=Quantidade]').value) ?? 0;
        const subtotalEmCentavos = emCentavos(valor) * quantidade;

        item.querySelector('[data-campo=Subtotal]').textContent = formatarMoeda(subtotalEmCentavos / 100);
        totalEmCentavos += subtotalEmCentavos;
        if (item.querySelector('[data-campo=ProdutoId]').value) {
            itens += 1;
            unidades += quantidade;
        }
    });

    mostrarNoVisor(formatarDecimal(totalEmCentavos / 100));
    itensDaVenda.textContent = formatarContagem(itens, configuracao.textoItem, configuracao.textoItens);
    unidadesDaVenda.textContent = formatarContagem(unidades, configuracao.textoUnidade, configuracao.textoUnidades);
}

function mostrarNoVisor(valor) {
    if (valorNoVisor.textContent === valor) {
        return;
    }

    valorNoVisor.textContent = valor;
    if (!movimentoReduzido.matches) {
        totalDaVenda.animate([{ opacity: 0.45 }, { opacity: 1 }], { duration: 180, easing: 'ease-out' });
    }
}
