const chaveDoTema = 'sophia-tema';
const temas = {
    claro: { icone: '#icone-sol' },
    escuro: { icone: '#icone-lua' },
    automatico: { icone: '#icone-automatico' },
};
const sistemaEscuro = window.matchMedia('(prefers-color-scheme: dark)');
const botao = document.querySelector('[data-tema=botao]');
const icone = document.querySelector('[data-tema=icone]');
const opcoes = document.querySelectorAll('[data-tema-opcao]');

const lerPreferencia = () => {
    try {
        const guardada = localStorage.getItem(chaveDoTema);
        return Object.hasOwn(temas, guardada) ? guardada : 'automatico';
    } catch {
        return 'automatico';
    }
};

const guardarPreferencia = preferencia => {
    try {
        localStorage.setItem(chaveDoTema, preferencia);
        return true;
    } catch {
        return false;
    }
};

const pintar = escuro => {
    document.documentElement.setAttribute('data-bs-theme', escuro ? 'dark' : 'light');
};

const aplicar = preferencia => {
    pintar(preferencia === 'escuro' || (preferencia === 'automatico' && sistemaEscuro.matches));

    const tema = temas[preferencia];
    const nomeDoTema = document.querySelector(`[data-tema-opcao=${preferencia}]`)?.dataset.temaNome ?? preferencia;
    botao?.setAttribute('aria-label', botao.dataset.rotulo.replace('{tema}', nomeDoTema));
    icone?.setAttribute('href', tema.icone);
    opcoes.forEach(opcao => {
        const escolhida = opcao.dataset.temaOpcao === preferencia;
        if (opcao.getAttribute('role') === 'radio') {
            opcao.setAttribute('aria-checked', String(escolhida));
            opcao.tabIndex = escolhida ? 0 : -1;
        } else {
            opcao.setAttribute('aria-pressed', String(escolhida));
            opcao.classList.toggle('active', escolhida);
        }
    });
};

const escolher = opcao => {
    guardarPreferencia(opcao.dataset.temaOpcao);
    aplicar(opcao.dataset.temaOpcao);
};

opcoes.forEach(opcao => {
    opcao.addEventListener('click', () => {
        escolher(opcao);
        if (opcao.getAttribute('role') !== 'radio') {
            botao?.focus();
        }
    });
});

const passoDaSeta = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 };
document.querySelector('[data-tema=grupo]')?.addEventListener('keydown', evento => {
    const passo = passoDaSeta[evento.key];
    if (!passo) {
        return;
    }

    evento.preventDefault();
    const radios = [...evento.currentTarget.querySelectorAll('[role=radio]')];
    const atual = radios.indexOf(document.activeElement);
    const proximo = radios[(atual + passo + radios.length) % radios.length];
    escolher(proximo);
    proximo.focus();
});

sistemaEscuro.addEventListener('change', () => aplicar(lerPreferencia()));
window.addEventListener('storage', evento => {
    if (evento.key === chaveDoTema) {
        aplicar(lerPreferencia());
    }
});
window.addEventListener('beforeprint', () => pintar(false));
window.addEventListener('afterprint', () => aplicar(lerPreferencia()));

aplicar(lerPreferencia());
