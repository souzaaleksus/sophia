import { escritaDecimalValida, lerDecimal } from './formato.js';

if (window.jQuery?.validator) {
    jQuery.validator.methods.number = function (valor, elemento) {
        return this.optional(elemento) || escritaDecimalValida(valor);
    };

    jQuery.validator.addMethod('maximo', function (valor, elemento) {
        return this.optional(elemento) || !escritaDecimalValida(valor) || lerDecimal(valor) !== null;
    });
    jQuery.validator.unobtrusive.adapters.addBool('maximo');

    jQuery.validator.methods.range = function (valor, elemento, limites) {
        const numero = lerDecimal(valor);
        return this.optional(elemento) || (numero !== null && numero >= limites[0] && numero <= limites[1]);
    };

    const digitoVerificador = digitos => {
        const soma = digitos.reduce((total, digito, posicao) => total + digito * (digitos.length + 1 - posicao), 0);
        const resto = (soma * 10) % 11;
        return resto === 10 ? 0 : resto;
    };

    const cpfValido = texto => {
        const digitos = [...String(texto).replace(/\D/g, '')].map(Number);
        if (digitos.length !== 11 || new Set(digitos).size === 1) {
            return false;
        }

        const primeiro = digitoVerificador(digitos.slice(0, 9));
        const segundo = digitoVerificador([...digitos.slice(0, 9), primeiro]);
        return primeiro === digitos[9] && segundo === digitos[10];
    };

    jQuery.validator.addMethod('cpf', function (valor, elemento) {
        return this.optional(elemento) || cpfValido(valor);
    });
    jQuery.validator.unobtrusive.adapters.addBool('cpf');
}
