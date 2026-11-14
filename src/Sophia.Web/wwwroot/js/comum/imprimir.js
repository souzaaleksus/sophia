document.querySelectorAll('[data-acao=imprimir]').forEach(botao => {
    botao.addEventListener('click', () => window.print());
});
