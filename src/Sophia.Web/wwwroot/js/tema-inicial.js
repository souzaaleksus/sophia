(() => {
    const lerTemaGuardado = () => {
        try {
            return localStorage.getItem('sophia-tema');
        } catch {
            return null;
        }
    };
    const preferencia = lerTemaGuardado() ?? 'automatico';
    const sistemaEscuro = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const escuro = preferencia === 'escuro' || (preferencia !== 'claro' && sistemaEscuro);
    document.documentElement.setAttribute('data-bs-theme', escuro ? 'dark' : 'light');
})();
