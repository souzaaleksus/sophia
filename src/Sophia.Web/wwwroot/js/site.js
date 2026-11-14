(() => {
    const mascaras = {
        cpf: digitos => digitos
            .slice(0, 11)
            .replace(/^(\d{3})(\d)/, '$1.$2')
            .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
            .replace(/\.(\d{3})(\d{1,2})$/, '.$1-$2'),
        telefone: digitos => {
            const numero = digitos.slice(0, 11);
            if (numero.length <= 2) {
                return numero.length > 0 ? `(${numero}` : '';
            }

            const corte = numero.length === 11 ? 7 : 6;
            const meio = numero.slice(2, corte);
            const fim = numero.slice(corte);
            return `(${numero.slice(0, 2)}) ${meio}${fim ? `-${fim}` : ''}`;
        },
    };

    document.querySelectorAll('[data-mascara]').forEach(campo => {
        const aplicar = mascaras[campo.dataset.mascara];
        const formatar = () => {
            campo.value = aplicar(campo.value.replace(/\D/g, ''));
        };
        campo.addEventListener('input', formatar);
        formatar();
    });

    document.querySelectorAll('[data-acao=imprimir]').forEach(botao => {
        botao.addEventListener('click', () => window.print());
    });
})();

(() => {
    const chaveDoTema = 'sophia-tema';
    const temas = {
        claro: { nome: 'claro', icone: '#icone-sol' },
        escuro: { nome: 'escuro', icone: '#icone-lua' },
        automatico: { nome: 'automático', icone: '#icone-automatico' },
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
        } catch {
            return;
        }
    };

    const pintar = escuro => {
        document.documentElement.setAttribute('data-bs-theme', escuro ? 'dark' : 'light');
    };

    const aplicar = preferencia => {
        pintar(preferencia === 'escuro' || (preferencia === 'automatico' && sistemaEscuro.matches));

        const tema = temas[preferencia];
        botao?.setAttribute('aria-label', `Escolher o tema (atual: ${tema.nome})`);
        icone?.setAttribute('href', tema.icone);
        opcoes.forEach(opcao => {
            const escolhida = opcao.dataset.temaOpcao === preferencia;
            opcao.setAttribute('aria-pressed', String(escolhida));
            opcao.classList.toggle('active', escolhida);
        });
    };

    opcoes.forEach(opcao => {
        opcao.addEventListener('click', () => {
            guardarPreferencia(opcao.dataset.temaOpcao);
            aplicar(opcao.dataset.temaOpcao);
            botao?.focus();
        });
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
})();

(() => {
    const tempoNaTela = 6000;
    const movimentoReduzido = window.matchMedia('(prefers-reduced-motion: reduce)');

    const fechar = aviso => {
        if (aviso.contains(document.activeElement)) {
            document.getElementById('conteudo')?.focus();
        }

        if (movimentoReduzido.matches) {
            aviso.remove();
            return;
        }

        aviso.animate(
            [{ opacity: 1, transform: 'none' }, { opacity: 0, transform: 'translateY(-0.5rem)' }],
            { duration: 220, easing: 'ease-in' },
        ).finished.then(() => aviso.remove());
    };

    document.querySelectorAll('[data-aviso]').forEach(aviso => {
        aviso.querySelector('[data-acao=fechar-aviso]')?.addEventListener('click', () => fechar(aviso));
    });

    document.querySelectorAll('[data-aviso=temporario]').forEach(aviso => {
        const fecharQuandoLivre = () => {
            if (aviso.matches(':hover, :focus-within')) {
                window.setTimeout(fecharQuandoLivre, 1000);
                return;
            }

            fechar(aviso);
        };

        window.setTimeout(fecharQuandoLivre, tempoNaTela);
    });
})();
