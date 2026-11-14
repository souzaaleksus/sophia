const esperaAntesDeBuscar = 200;
const esperaAntesDeAvisarBusca = 150;

export function criarAutocompletar(entrada, { buscar, descrever, aoEscolher, aoDigitar, vazio = 'Nada encontrado.', cadastro }) {
    const lista = entrada.parentElement.querySelector('[role=listbox]');
    const aviso = document.createElement('div');
    aviso.className = 'opcoes opcoes-aviso';
    aviso.hidden = true;
    lista.after(aviso);
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
        if (evento.key === 'Escape') {
            fechar();
            return;
        }

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
        }
    });

    entrada.addEventListener('blur', () => setTimeout(fechar, 150));

    async function pesquisar(termo) {
        const numeroDaBusca = ++ultimaBusca;
        const ehAtual = () => numeroDaBusca === ultimaBusca && document.activeElement === entrada;
        const avisoDeBusca = setTimeout(() => {
            if (ehAtual()) {
                avisar('Buscando…', 'status');
                entrada.setAttribute('aria-busy', 'true');
            }
        }, esperaAntesDeAvisarBusca);

        let itens;
        try {
            itens = await buscar(termo);
        } catch {
            clearTimeout(avisoDeBusca);
            if (ehAtual()) {
                avisar('Não foi possível buscar. Tente de novo.', 'alert');
            }

            return;
        } finally {
            entrada.removeAttribute('aria-busy');
        }

        clearTimeout(avisoDeBusca);
        if (ehAtual()) {
            mostrar(itens);
        }
    }

    function mostrar(itens) {
        if (itens.length === 0) {
            avisar(vazio, 'status', cadastro);
            return;
        }

        esconderAviso();
        encontrados = itens;
        lista.replaceChildren(...itens.map(criarOpcao));
        lista.hidden = false;
        entrada.setAttribute('aria-expanded', 'true');
        destacar(0);
    }

    function avisar(texto, papel, atalho) {
        fecharLista();
        const mensagem = document.createElement('p');
        mensagem.className = 'opcoes-mensagem';
        mensagem.textContent = texto;
        aviso.replaceChildren(mensagem);
        if (atalho) {
            const link = document.createElement('a');
            link.className = 'opcoes-atalho';
            link.href = atalho.endereco;
            link.target = '_blank';
            link.rel = 'noopener';
            link.textContent = atalho.texto;
            link.addEventListener('mousedown', evento => evento.preventDefault());
            aviso.append(link);
        }

        aviso.setAttribute('role', papel);
        aviso.hidden = false;
    }

    function esconderAviso() {
        aviso.hidden = true;
        aviso.replaceChildren();
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
        esconderAviso();
        fecharLista();
    }

    function fecharLista() {
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
