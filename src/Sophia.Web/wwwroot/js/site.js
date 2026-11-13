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
