const esperaAntesDeBuscar = 200;

export function criarAutocompletar(entrada, { buscar, descrever, aoEscolher, aoDigitar }) {
    const lista = entrada.parentElement.querySelector('[role=listbox]');
    let encontrados = [];
    let ativo = -1;
    let temporizador;
    let ultimaBusca = 0;

    entrada.addEventListener('input', () => {
        aoDigitar?.();
        clearTimeout(temporizador);
        const termo = entrada.value.trim();
        if (termo === '') {
            fechar();
            return;
        }

        temporizador = setTimeout(() => pesquisar(termo), esperaAntesDeBuscar);
    });

    entrada.addEventListener('keydown', evento => {
        if (lista.hidden) {
            return;
        }

        if (evento.key === 'ArrowDown') {
            evento.preventDefault();
            destacar(Math.min(ativo + 1, encontrados.length - 1));
        } else if (evento.key === 'ArrowUp') {
            evento.preventDefault();
            destacar(Math.max(ativo - 1, 0));
        } else if (evento.key === 'Enter' && !evento.ctrlKey && !evento.metaKey && ativo >= 0) {
            evento.preventDefault();
            evento.stopPropagation();
            escolher(ativo);
        } else if (evento.key === 'Escape') {
            fechar();
        }
    });

    entrada.addEventListener('blur', () => setTimeout(fechar, 150));

    async function pesquisar(termo) {
        const numeroDaBusca = ++ultimaBusca;
        let itens = [];
        try {
            itens = await buscar(termo);
        } catch {
            itens = [];
        }

        if (numeroDaBusca === ultimaBusca && document.activeElement === entrada) {
            mostrar(itens);
        }
    }

    function mostrar(itens) {
        encontrados = itens;
        lista.replaceChildren(...(itens.length > 0 ? itens.map(criarOpcao) : [criarAvisoDeVazio()]));
        lista.hidden = false;
        entrada.setAttribute('aria-expanded', 'true');
        destacar(itens.length > 0 ? 0 : -1);
    }

    function criarOpcao(item, indice) {
        const { principal, secundario } = descrever(item);
        const opcao = document.createElement('li');
        opcao.id = `${lista.id}-${indice}`;
        opcao.setAttribute('role', 'option');
        opcao.className = 'opcao';

        const textoPrincipal = document.createElement('span');
        textoPrincipal.textContent = principal;
        const textoSecundario = document.createElement('span');
        textoSecundario.className = 'secundario';
        textoSecundario.textContent = secundario;
        opcao.append(textoPrincipal, textoSecundario);

        opcao.addEventListener('mousedown', evento => {
            evento.preventDefault();
            escolher(indice);
        });
        return opcao;
    }

    function criarAvisoDeVazio() {
        const aviso = document.createElement('li');
        aviso.className = 'opcao vazia';
        aviso.textContent = 'Nada encontrado.';
        return aviso;
    }

    function destacar(indice) {
        ativo = indice;
        lista.querySelectorAll('[role=option]').forEach((opcao, posicao) => {
            opcao.setAttribute('aria-selected', String(posicao === indice));
        });

        if (indice >= 0) {
            entrada.setAttribute('aria-activedescendant', `${lista.id}-${indice}`);
            document.getElementById(`${lista.id}-${indice}`)?.scrollIntoView({ block: 'nearest' });
        } else {
            entrada.removeAttribute('aria-activedescendant');
        }
    }

    function escolher(indice) {
        const item = encontrados[indice];
        fechar();
        if (item) {
            aoEscolher(item);
        }
    }

    function fechar() {
        lista.hidden = true;
        lista.replaceChildren();
        entrada.setAttribute('aria-expanded', 'false');
        entrada.removeAttribute('aria-activedescendant');
        encontrados = [];
        ativo = -1;
    }
}

export async function buscarJson(endereco, termo) {
    const resposta = await fetch(`${endereco}?termo=${encodeURIComponent(termo)}`, {
        headers: { Accept: 'application/json' },
    });
    if (!resposta.ok) {
        throw new Error(`Busca falhou: ${resposta.status}`);
    }

    return resposta.json();
}
