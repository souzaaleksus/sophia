const esperaAntesDeBuscar = 200;
const esperaAntesDeAvisarBusca = 150;
const esperaAntesDeFecharAoSair = 150;

export function criarAutocompletar(entrada, opcoes) {
    return new Autocompletar(entrada, opcoes);
}

class Autocompletar {
    #entrada;
    #lista;
    #aviso;
    #opcoes;
    #encontrados = [];
    #ativo = -1;
    #temporizador;
    #ultimaBusca = 0;

    constructor(entrada, opcoes) {
        this.#entrada = entrada;
        this.#opcoes = opcoes;
        this.#lista = entrada.parentElement.querySelector('[role=listbox]');
        this.#aviso = criarAreaDeAviso(this.#lista);
        entrada.addEventListener('input', () => this.#aoDigitar());
        entrada.addEventListener('keydown', evento => this.#aoTeclar(evento));
        entrada.addEventListener('blur', () => setTimeout(() => this.#fechar(), esperaAntesDeFecharAoSair));
    }

    #aoDigitar() {
        this.#opcoes.aoDigitar?.();
        clearTimeout(this.#temporizador);
        const termo = this.#entrada.value.trim();
        if (termo === '') {
            this.#fechar();
            return;
        }

        this.#temporizador = setTimeout(() => this.#pesquisar(termo), esperaAntesDeBuscar);
    }

    #aoTeclar(evento) {
        if (evento.key === 'Escape') {
            this.#fechar();
        } else if (!this.#lista.hidden) {
            this.#navegar(evento);
        }
    }

    #navegar(evento) {
        const passos = { ArrowDown: 1, ArrowUp: -1 };
        if (evento.key in passos) {
            evento.preventDefault();
            const proximo = this.#ativo + passos[evento.key];
            this.#destacar(Math.min(Math.max(proximo, 0), this.#encontrados.length - 1));
        } else if (evento.key === 'Enter' && !evento.ctrlKey && !evento.metaKey && this.#ativo >= 0) {
            evento.preventDefault();
            evento.stopPropagation();
            this.#escolher(this.#ativo);
        }
    }

    async #pesquisar(termo) {
        const numeroDaBusca = ++this.#ultimaBusca;
        const ehAtual = () => numeroDaBusca === this.#ultimaBusca && document.activeElement === this.#entrada;
        const avisoDeBusca = setTimeout(() => this.#avisarQueEstaBuscando(ehAtual), esperaAntesDeAvisarBusca);
        try {
            const itens = await this.#opcoes.buscar(termo);
            if (ehAtual()) {
                this.#mostrar(itens);
            }
        } catch {
            if (ehAtual()) {
                this.#avisar(this.#opcoes.textos.falha, 'alert');
            }
        } finally {
            clearTimeout(avisoDeBusca);
            this.#entrada.removeAttribute('aria-busy');
        }
    }

    #avisarQueEstaBuscando(ehAtual) {
        if (ehAtual()) {
            this.#avisar(this.#opcoes.textos.buscando, 'status');
            this.#entrada.setAttribute('aria-busy', 'true');
        }
    }

    #mostrar(itens) {
        if (itens.length === 0) {
            this.#avisar(this.#opcoes.textos.vazio, 'status', this.#opcoes.cadastro);
            return;
        }

        this.#esconderAviso();
        this.#encontrados = itens;
        this.#lista.replaceChildren(...itens.map((item, indice) => this.#criarOpcao(item, indice)));
        this.#lista.hidden = false;
        this.#entrada.setAttribute('aria-expanded', 'true');
        this.#destacar(0);
    }

    #avisar(texto, papel, atalho) {
        this.#fecharLista();
        this.#aviso.replaceChildren(criarMensagem(texto), ...(atalho ? [criarAtalho(atalho)] : []));
        this.#aviso.setAttribute('role', papel);
        this.#aviso.hidden = false;
    }

    #esconderAviso() {
        this.#aviso.hidden = true;
        this.#aviso.replaceChildren();
    }

    #criarOpcao(item, indice) {
        const { principal, secundario } = this.#opcoes.descrever(item);
        const opcao = document.createElement('li');
        opcao.id = `${this.#lista.id}-${indice}`;
        opcao.setAttribute('role', 'option');
        opcao.className = 'opcao';
        opcao.append(criarTexto(principal), criarTexto(secundario, 'secundario'));
        opcao.addEventListener('mousedown', evento => {
            evento.preventDefault();
            this.#escolher(indice);
        });
        return opcao;
    }

    #destacar(indice) {
        this.#ativo = indice;
        this.#lista.querySelectorAll('[role=option]').forEach((opcao, posicao) => {
            opcao.setAttribute('aria-selected', String(posicao === indice));
        });

        if (indice < 0) {
            this.#entrada.removeAttribute('aria-activedescendant');
            return;
        }

        const id = `${this.#lista.id}-${indice}`;
        this.#entrada.setAttribute('aria-activedescendant', id);
        document.getElementById(id)?.scrollIntoView({ block: 'nearest' });
    }

    #escolher(indice) {
        const item = this.#encontrados[indice];
        this.#fechar();
        if (item) {
            this.#opcoes.aoEscolher(item);
        }
    }

    #fechar() {
        this.#esconderAviso();
        this.#fecharLista();
    }

    #fecharLista() {
        this.#lista.hidden = true;
        this.#lista.replaceChildren();
        this.#entrada.setAttribute('aria-expanded', 'false');
        this.#entrada.removeAttribute('aria-activedescendant');
        this.#encontrados = [];
        this.#ativo = -1;
    }
}

function criarAreaDeAviso(lista) {
    const aviso = document.createElement('div');
    aviso.className = 'opcoes opcoes-aviso';
    aviso.hidden = true;
    lista.after(aviso);
    return aviso;
}

function criarMensagem(texto) {
    const mensagem = document.createElement('p');
    mensagem.className = 'opcoes-mensagem';
    mensagem.textContent = texto;
    return mensagem;
}

function criarAtalho({ endereco, texto }) {
    const link = document.createElement('a');
    link.className = 'opcoes-atalho';
    link.href = endereco;
    link.target = '_blank';
    link.rel = 'noopener';
    link.textContent = texto;
    link.addEventListener('mousedown', evento => evento.preventDefault());
    return link;
}

function criarTexto(texto, classe) {
    const elemento = document.createElement('span');
    elemento.textContent = texto;
    if (classe) {
        elemento.className = classe;
    }

    return elemento;
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
