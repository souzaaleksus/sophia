const esperaAntesDeBuscar = 300;

document.querySelectorAll('form[data-busca-ao-vivo]').forEach(prepararBusca);

function prepararBusca(formulario) {
    const campo = formulario.querySelector('input[type=search]');
    const limpar = formulario.querySelector('[data-acao=limpar-busca]');
    const enviar = formulario.querySelector('[type=submit]');
    let temporizador;
    let ultimaBusca = 0;

    enviar.hidden = true;
    limpar.hidden = campo.value === '';

    campo.addEventListener('input', () => {
        limpar.hidden = campo.value === '';
        clearTimeout(temporizador);
        temporizador = setTimeout(atualizar, esperaAntesDeBuscar);
    });

    limpar.addEventListener('click', () => {
        campo.value = '';
        limpar.hidden = true;
        campo.focus();
        atualizar();
    });

    async function atualizar() {
        const numeroDaBusca = ++ultimaBusca;
        const endereco = enderecoDaBusca(formulario);
        try {
            const pagina = await carregar(endereco);
            if (numeroDaBusca === ultimaBusca) {
                formulario.querySelector('[data-falha-da-busca]').hidden = true;
                trocarResultados(pagina);
                history.replaceState(null, '', endereco);
            }
        } catch {
            if (numeroDaBusca === ultimaBusca) {
                avisarFalha(formulario);
            }
        }
    }
}

function enderecoDaBusca(formulario) {
    const filtros = new URLSearchParams(new FormData(formulario));
    [...filtros.keys()].filter(chave => filtros.get(chave) === '').forEach(chave => filtros.delete(chave));
    const consulta = filtros.toString();
    return consulta ? `${formulario.action}?${consulta}` : formulario.action;
}

async function carregar(endereco) {
    const resposta = await fetch(endereco, { headers: { Accept: 'text/html' } });
    if (!resposta.ok) {
        throw new Error(`Lista não carregou: ${resposta.status}`);
    }

    return new DOMParser().parseFromString(await resposta.text(), 'text/html');
}

function trocarResultados(pagina) {
    const atuais = document.querySelector('[data-resultados]');
    const novos = pagina.querySelector('[data-resultados]');
    if (atuais && novos) {
        atuais.replaceWith(document.importNode(novos, true));
    }
}

function avisarFalha(formulario) {
    const aviso = formulario.querySelector('[data-falha-da-busca]');
    aviso.hidden = false;
}
