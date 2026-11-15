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

const ateOTeto = (numero, teto) => (numero !== null && numero <= (teto ?? Infinity) ? numero : null);

export function lerLinha(item) {
    const campo = nome => item.querySelector(`[data-campo=${nome}]`);
    const valor = campo('ValorUnitario');
    const quantidade = campo('Quantidade');
    return {
        centavos: ateOTeto(lerCentavos(valor.value), lerCentavos(valor.dataset.valorMaximo)),
        quantidade: ateOTeto(lerInteiro(quantidade.value), lerInteiro(quantidade.max)),
        produtoEscolhido: campo('ProdutoId').value !== '',
    };
}

export function mostrarSubtotal(item, subtotalEmCentavos) {
    escreverQuebrandoNoMilhar(item.querySelector('[data-campo=Subtotal]'), formatarMoeda(subtotalEmCentavos / 100));
    const campo = item.querySelector('[data-campo=Quantidade]');
    const quantidade = lerInteiro(campo.value) ?? 0;
    item.querySelector('[data-acao=menos]').disabled = quantidade <= 1;
    item.querySelector('[data-acao=mais]').disabled = quantidade >= (lerInteiro(campo.max) ?? Infinity);
}

function escreverQuebrandoNoMilhar(elemento, texto) {
    const pedacos = texto.split('.').flatMap((parte, posicao, partes) =>
        posicao < partes.length - 1 ? [`${parte}.`, document.createElement('wbr')] : [parte]);
    elemento.replaceChildren(...pedacos);
}
