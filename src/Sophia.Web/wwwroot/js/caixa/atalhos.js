export function ligarAtalhos({ buscarCliente, lancarProduto, finalizar }) {
    const semModificador = evento => !evento.ctrlKey && !evento.altKey;
    const atalhos = [
        { combina: evento => evento.key === 'F2' && semModificador(evento), executar: buscarCliente },
        { combina: evento => evento.key === 'F4' && semModificador(evento), executar: lancarProduto },
        { combina: evento => evento.key === 'Enter' && (evento.ctrlKey || evento.metaKey), executar: finalizar },
    ];

    document.addEventListener('keydown', evento => {
        const atalho = atalhos.find(candidato => candidato.combina(evento));
        if (atalho) {
            evento.preventDefault();
            atalho.executar();
        }
    });
}
