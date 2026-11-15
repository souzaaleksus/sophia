const dialogo = document.getElementById('confirmar-exclusao');

if (dialogo) {
    const formulario = dialogo.querySelector('form');
    const parte = nome => dialogo.querySelector(`[data-dialogo=${nome}]`);
    const cancelar = parte('cancelar');
    let origem = null;

    const abrir = link => {
        origem = link;
        parte('titulo').textContent = link.dataset.confirmarTitulo;
        parte('nome').textContent = link.dataset.confirmarNome;
        parte('detalhe').textContent = link.dataset.confirmarDetalhe ?? '';
        formulario.action = link.href;
        dialogo.showModal();
        cancelar.focus();
    };

    document.addEventListener('click', evento => {
        const link = evento.target.closest('a[data-confirmar]');
        if (link) {
            evento.preventDefault();
            abrir(link);
        }
    });

    const enviando = () => formulario.dataset.enviando === 'true';

    dialogo.addEventListener('cancel', evento => {
        if (enviando()) {
            evento.preventDefault();
        }
    });
    cancelar.addEventListener('click', () => {
        if (!enviando()) {
            dialogo.close();
        }
    });
    dialogo.addEventListener('close', () => origem?.focus());
}
