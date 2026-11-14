(() => {
    if (!window.jQuery || !jQuery.validator) {
        return;
    }

    const numeroBrasileiro = /^-?(\d{1,3}(\.\d{3})*|\d+)(,\d+)?$/;
    const paraNumero = valor => Number(String(valor).replace(/\./g, '').replace(',', '.'));

    jQuery.validator.methods.number = function (valor, elemento) {
        return this.optional(elemento) || numeroBrasileiro.test(String(valor).trim());
    };

    jQuery.validator.methods.range = function (valor, elemento, limites) {
        const numero = paraNumero(valor);
        return this.optional(elemento) || (numero >= limites[0] && numero <= limites[1]);
    };
})();
