import { formatarContagem, formatarDecimal, formatarMoeda, lerCentavos, lerInteiro } from '../formato.js';

export function somarLinhas(linhas) {
    return linhas.reduce((soma, linha) => {
        const subtotalEmCentavos = (linha.centavos ?? 0) * (linha.quantidade ?? 0);
        const lancada = linha.produtoEscolhido;
        return {
            totalEmCentavos: soma.totalEmCentavos + subtotalEmCentavos,
            itens: soma.itens + (lancada ? 1 : 0),
            unidades: soma.unidades + (lancada ? linha.quantidade ?? 0 : 0),
            subtotaisEmCentavos: [...soma.subtotaisEmCentavos, subtotalEmCentavos],
        };
    }, { totalEmCentavos: 0, itens: 0, unidades: 0, subtotaisEmCentavos: [] });
}

export function criarVisor(formulario, textos, movimentoReduzido) {
    const total = formulario.querySelector('[data-testid=total-da-venda]');
    const valor = total.querySelector('[data-visor=valor]');
    const itens = formulario.querySelector('[data-testid=itens-da-venda]');
    const unidades = formulario.querySelector('[data-testid=unidades-da-venda]');

    function mostrar(soma) {
        const texto = formatarDecimal(soma.totalEmCentavos / 100);
        if (valor.textContent !== texto) {
            escreverQuebrandoNoMilhar(valor, texto);
            piscar();
        }

        itens.textContent = formatarContagem(soma.itens, textos.textoItem, textos.textoItens);
        unidades.textContent = formatarContagem(soma.unidades, textos.textoUnidade, textos.textoUnidades);
    }

    function piscar() {
        if (!movimentoReduzido.matches) {
            total.animate([{ opacity: 0.45 }, { opacity: 1 }], { duration: 180, easing: 'ease-out' });
        }
    }

    return { mostrar };
}

export function lerLinha(item) {
    const campo = nome => item.querySelector(`[data-campo=${nome}]`);
    const valor = campo('ValorUnitario');
    const centavos = lerCentavos(valor.value);
    return {
        centavos: centavos !== null && centavos <= lerCentavos(valor.dataset.valorMaximo) ? centavos : null,
        quantidade: lerInteiro(campo('Quantidade').value),
        produtoEscolhido: campo('ProdutoId').value !== '',
    };
}

export function mostrarSubtotal(item, subtotalEmCentavos, quantidade) {
    escreverQuebrandoNoMilhar(item.querySelector('[data-campo=Subtotal]'), formatarMoeda(subtotalEmCentavos / 100));
    const maximo = lerInteiro(item.querySelector('[data-campo=Quantidade]').max) ?? Infinity;
    item.querySelector('[data-acao=menos]').disabled = (quantidade ?? 0) <= 1;
    item.querySelector('[data-acao=mais]').disabled = (quantidade ?? 0) >= maximo;
}

function escreverQuebrandoNoMilhar(elemento, texto) {
    const pedacos = texto.split('.').flatMap((parte, posicao, partes) =>
        posicao < partes.length - 1 ? [`${parte}.`, document.createElement('wbr')] : [parte]);
    elemento.replaceChildren(...pedacos);
}
