import { buscarJson, criarAutocompletar } from './autocompletar.js';
import { emCentavos, formatarDecimal, formatarMoeda, lerDecimal, lerInteiro } from './formato.js';

const formulario = document.getElementById('nova-venda');
const areaDosItens = formulario.querySelector('[data-itens]');
const modeloDeItem = document.getElementById('modelo-de-item');
const totalDaVenda = formulario.querySelector('[data-testid=total-da-venda]');
const quantidadeDeItens = formulario.querySelector('[data-resumo=itens]');

prepararCliente();
areaDosItens.querySelectorAll('[data-item]').forEach(prepararItem);
renumerarItens();
recalcularTotais();

formulario.querySelector('[data-acao=adicionar-item]').addEventListener('click', () => adicionarItem().focus());

formulario.addEventListener('keydown', evento => {
    if (evento.key !== 'Enter' || evento.target.tagName !== 'INPUT') {
        return;
    }

    evento.preventDefault();
    if (evento.ctrlKey) {
        formulario.requestSubmit();
    }
});

formulario.addEventListener('submit', () => {
    descartarLinhasEmBranco();
    renumerarItens();
});

function prepararCliente() {
    const campo = nome => formulario.querySelector(`[data-cliente=${nome}]`);
    const busca = campo('busca');
    const escolhido = campo('escolhido');
    const entrada = busca.querySelector('input');

    criarAutocompletar(entrada, {
        buscar: termo => buscarJson('/Clientes/Buscar', termo),
        descrever: cliente => ({ principal: cliente.nome, secundario: cliente.cpf }),
        aoEscolher: cliente => {
            campo('id').value = cliente.id;
            campo('nome').value = cliente.nome;
            campo('cpf').value = cliente.cpf;
            campo('nome-exibido').textContent = cliente.nome;
            campo('cpf-exibido').textContent = cliente.cpf;
            busca.hidden = true;
            escolhido.hidden = false;
            areaDosItens.querySelector('[data-campo=ProdutoNome]')?.focus();
        },
    });

    campo('trocar').addEventListener('click', () => {
        campo('id').value = '';
        campo('nome').value = '';
        campo('cpf').value = '';
        escolhido.hidden = true;
        busca.hidden = false;
        entrada.value = '';
        entrada.focus();
    });
}

function adicionarItem() {
    const item = modeloDeItem.content.firstElementChild.cloneNode(true);
    areaDosItens.append(item);
    prepararItem(item);
    renumerarItens();
    recalcularTotais();
    return item.querySelector('[data-campo=ProdutoNome]');
}

function prepararItem(item) {
    const campo = nome => item.querySelector(`[data-campo=${nome}]`);

    criarAutocompletar(campo('ProdutoNome'), {
        buscar: termo => buscarJson('/Produtos/Buscar', termo),
        descrever: produto => ({ principal: produto.nome, secundario: formatarMoeda(produto.valorUnitario) }),
        aoDigitar: () => {
            campo('ProdutoId').value = '';
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
        if (evento.key === 'Enter' && !evento.ctrlKey) {
            evento.preventDefault();
            evento.stopPropagation();
            adicionarItem().focus();
        }
    });

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

function removerItem(item) {
    item.remove();
    if (!areaDosItens.querySelector('[data-item]')) {
        adicionarItem();
    }

    renumerarItens();
    recalcularTotais();
}

function descartarLinhasEmBranco() {
    areaDosItens.querySelectorAll('[data-item]').forEach(item => {
        const preenchidos = ['ProdutoId', 'ProdutoNome', 'ValorUnitario', 'Quantidade']
            .map(nome => item.querySelector(`[data-campo=${nome}]`).value.trim())
            .filter(valor => valor !== '');
        if (preenchidos.length === 0) {
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
    let lancados = 0;

    areaDosItens.querySelectorAll('[data-item]').forEach(item => {
        const valor = lerDecimal(item.querySelector('[data-campo=ValorUnitario]').value) ?? 0;
        const quantidade = lerInteiro(item.querySelector('[data-campo=Quantidade]').value) ?? 0;
        const subtotalEmCentavos = emCentavos(valor) * quantidade;

        item.querySelector('[data-campo=Subtotal]').textContent = formatarMoeda(subtotalEmCentavos / 100);
        totalEmCentavos += subtotalEmCentavos;
        if (item.querySelector('[data-campo=ProdutoId]').value) {
            lancados += 1;
        }
    });

    totalDaVenda.textContent = formatarMoeda(totalEmCentavos / 100);
    quantidadeDeItens.textContent = lancados;
}
