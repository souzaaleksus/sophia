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
        data: digitos => digitos
            .slice(0, 8)
            .replace(/^(\d{2})(\d)/, '$1/$2')
            .replace(/^(\d{2})\/(\d{2})(\d)/, '$1/$2/$3'),
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

(() => {
    const campoDoErro = erro => {
        const nome = erro.dataset.valmsgFor;
        const direto = nome ? document.querySelector(`[name="${CSS.escape(nome)}"]:not([type=hidden])`) : null;
        return direto ?? erro.closest('.campo, section')?.querySelector('input:not([type=hidden]), select, textarea');
    };

    let primeiro = null;
    document.querySelectorAll('[data-valmsg-for]').forEach((erro, posicao) => {
        if (erro.textContent.trim() === '') {
            return;
        }

        const campo = campoDoErro(erro);
        if (!campo || campo.closest('[hidden]')) {
            return;
        }

        erro.id ||= `erro-${posicao}`;
        const descricoes = new Set((campo.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean));
        descricoes.add(erro.id);
        campo.setAttribute('aria-describedby', [...descricoes].join(' '));
        campo.setAttribute('aria-invalid', 'true');
        primeiro ??= campo;
    });

    primeiro?.focus();
})();

(() => {
    const conteudoOriginal = new WeakMap();

    const travar = (formulario, botao) => {
        formulario.dataset.enviando = 'true';
        if (!botao) {
            return;
        }

        conteudoOriginal.set(botao, botao.innerHTML);
        botao.disabled = true;
        botao.setAttribute('aria-busy', 'true');
        botao.textContent = formulario.dataset.textoEnviando;
    };

    const liberar = formulario => {
        delete formulario.dataset.enviando;
        formulario.querySelectorAll('[aria-busy=true]').forEach(botao => {
            botao.disabled = false;
            botao.removeAttribute('aria-busy');
            botao.innerHTML = conteudoOriginal.get(botao) ?? botao.innerHTML;
        });
    };

    window.addEventListener('submit', evento => {
        const formulario = evento.target;
        if (!(formulario instanceof HTMLFormElement) || !formulario.hasAttribute('data-envio')) {
            return;
        }

        if (formulario.dataset.enviando === 'true') {
            evento.preventDefault();
            return;
        }

        if (!evento.defaultPrevented) {
            travar(formulario, evento.submitter ?? formulario.querySelector('[type=submit]'));
        }
    });

    window.addEventListener('pageshow', evento => {
        if (evento.persisted) {
            document.querySelectorAll('form[data-envio]').forEach(liberar);
        }
    });
})();
