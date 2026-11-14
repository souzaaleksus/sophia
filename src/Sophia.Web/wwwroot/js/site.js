(() => {
    const mascaras = {
        cpf: {
            limite: 11,
            formatar: digitos => digitos
                .replace(/^(\d{3})(\d)/, '$1.$2')
                .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
                .replace(/\.(\d{3})(\d{1,2})$/, '.$1-$2'),
        },
        telefone: {
            limite: 11,
            normalizar: (_, digitos) => (digitos.length > 11 && digitos.startsWith('55') ? digitos.slice(2) : digitos),
            formatar: formatarTelefone,
        },
        data: {
            limite: 8,
            normalizar: (texto, digitos) => {
                const iso = /^(\d{4})-(\d{2})-(\d{2})$/.exec(texto.trim());
                return iso ? `${iso[3]}${iso[2]}${iso[1]}` : digitos;
            },
            formatar: digitos => digitos
                .replace(/^(\d{2})(\d)/, '$1/$2')
                .replace(/^(\d{2})\/(\d{2})(\d)/, '$1/$2/$3'),
        },
    };

    function formatarTelefone(numero) {
        if (numero.length <= 2) {
            return numero.length > 0 ? `(${numero}` : '';
        }

        const corte = numero.length === 11 ? 7 : 6;
        const meio = numero.slice(2, corte);
        const fim = numero.slice(corte);
        return `(${numero.slice(0, 2)}) ${meio}${fim ? `-${fim}` : ''}`;
    }

    const contarDigitos = texto => texto.replace(/\D/g, '').length;

    function posicaoDepoisDosDigitos(texto, quantos) {
        let vistos = 0;
        for (let posicao = 0; posicao < texto.length && quantos > 0; posicao++) {
            if (/\d/.test(texto[posicao])) {
                vistos += 1;
            }

            if (vistos === quantos) {
                return posicao + 1;
            }
        }

        return quantos === 0 ? 0 : texto.length;
    }

    function apagarAntesDoSeparador(evento, digitos, antes, anterior) {
        const apagouSoOSeparador = evento?.inputType === 'deleteContentBackward' && digitos === anterior.digitos && antes > 0;
        return apagouSoOSeparador
            ? { digitos: digitos.slice(0, antes - 1) + digitos.slice(antes), antes: antes - 1 }
            : { digitos, antes };
    }

    function mascarar(mascara, campo, evento, anterior) {
        const texto = campo.value;
        const cursor = campo.selectionStart ?? texto.length;
        const normalizar = mascara.normalizar ?? ((_, digitos) => digitos);
        let { digitos, antes } = apagarAntesDoSeparador(
            evento,
            normalizar(texto, texto.replace(/\D/g, '')),
            contarDigitos(texto.slice(0, cursor)),
            anterior);

        if (digitos.length > mascara.limite) {
            if (evento?.inputType === 'insertText') {
                return { ...anterior, cursor: Math.max(0, cursor - (evento.data ?? '').length) };
            }

            digitos = digitos.slice(0, mascara.limite);
            antes = Math.min(antes, mascara.limite);
        }

        const valor = mascara.formatar(digitos);
        return { valor, digitos, cursor: posicaoDepoisDosDigitos(valor, antes) };
    }

    document.querySelectorAll('[data-mascara]').forEach(campo => {
        const mascara = mascaras[campo.dataset.mascara];
        let anterior = { valor: '', digitos: '', cursor: 0 };
        const aplicar = evento => {
            anterior = mascarar(mascara, campo, evento, anterior);
            campo.value = anterior.valor;
            if (document.activeElement === campo) {
                campo.setSelectionRange(anterior.cursor, anterior.cursor);
            }
        };
        campo.addEventListener('input', aplicar);
        aplicar();
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
