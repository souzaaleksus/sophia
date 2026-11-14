import { buscarJson, criarAutocompletar } from '../autocompletar.js';
import { limparErro } from '../comum/erros.js';
import { iniciais, tomDoAvatar } from '../formato.js';

export function criarEscolhaDeCliente(formulario, configuracao, textosDaBusca, aoEscolher) {
    const parte = nome => formulario.querySelector(`[data-cliente=${nome}]`);
    const busca = parte('busca');
    const escolhido = parte('escolhido');
    const entrada = busca.querySelector('input');
    let anterior = null;

    criarAutocompletar(entrada, {
        buscar: termo => buscarJson(configuracao.enderecoBuscaDeClientes, termo),
        descrever: cliente => ({ principal: cliente.nome, secundario: cliente.cpf }),
        textos: { ...textosDaBusca, vazio: configuracao.textoClienteNaoEncontrado },
        cadastro: { texto: configuracao.textoCadastrarCliente, endereco: configuracao.enderecoNovoCliente },
        aoEscolher: cliente => {
            anterior = null;
            mostrarEscolhido(cliente);
            aoEscolher();
        },
    });

    parte('trocar').addEventListener('click', buscarOutro);
    ligarDevolucao(busca, devolverAnterior, () => parte('trocar').focus());

    function mostrarEscolhido(cliente) {
        parte('id').value = cliente.id;
        parte('nome').value = cliente.nome;
        parte('cpf').value = cliente.cpf;
        parte('nome-exibido').textContent = cliente.nome;
        parte('cpf-exibido').textContent = cliente.cpf;
        mostrarAvatar(parte('avatar'), cliente.nome);
        busca.hidden = true;
        escolhido.hidden = false;
        limparErro(formulario, 'ClienteId');
    }

    function devolverAnterior() {
        const cliente = anterior;
        anterior = null;
        if (cliente) {
            entrada.value = '';
            mostrarEscolhido(cliente);
        }

        return cliente !== null;
    }

    function buscarOutro() {
        if (!escolhido.hidden) {
            anterior = { id: parte('id').value, nome: parte('nome').value, cpf: parte('cpf').value };
            ['id', 'nome', 'cpf'].forEach(nome => {
                parte(nome).value = '';
            });
            escolhido.hidden = true;
            busca.hidden = false;
            entrada.value = '';
        }

        entrada.focus();
        entrada.select();
    }

    return { buscarOutro };
}

function ligarDevolucao(busca, devolver, aoDevolverPeloTeclado) {
    busca.addEventListener('keydown', evento => {
        if (evento.key === 'Escape' && devolver()) {
            aoDevolverPeloTeclado();
        }
    });
    busca.addEventListener('focusout', evento => {
        if (!busca.contains(evento.relatedTarget) && document.hasFocus()) {
            devolver();
        }
    });
}

function mostrarAvatar(avatar, nome) {
    avatar.textContent = iniciais(nome);
    avatar.className = `avatar avatar-tom-${tomDoAvatar(nome)}`;
}
