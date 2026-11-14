import js from '@eslint/js';
import globals from 'globals';

export default [
    js.configs.recommended,
    {
        files: ['src/Sophia.Web/wwwroot/js/**/*.js'],
        ignores: ['src/Sophia.Web/wwwroot/lib/**'],
        languageOptions: {
            ecmaVersion: 2023,
            sourceType: 'module',
            globals: { ...globals.browser, jQuery: 'readonly' },
        },
        rules: {
            complexity: ['error', 10],
            'max-depth': ['error', 3],
            'max-lines-per-function': ['error', { max: 60, skipBlankLines: true }],
            'max-params': ['error', 4],
            'no-empty': 'error',
            'no-unused-vars': 'error',
            'prefer-const': 'error',
            eqeqeq: 'error',
        },
    },
    {
        files: ['src/Sophia.Web/wwwroot/js/validacao-pt-br.js', 'src/Sophia.Web/wwwroot/js/tema-inicial.js'],
        languageOptions: { sourceType: 'script' },
    },
];
