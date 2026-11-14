import { buscarJson, criarAutocompletar } from '../autocompletar.js';
import { iniciais, tomDoAvatar } from '../formato.js';

export function criarEscolhaDeCliente(formulario, configuracao, textosDaBusca, aoEscolher) {
    const parte = nome => formulario.querySelector(`[data-cliente=${nome}]`);
    const busca = parte('busca');
    const escolhido = parte('escolhido');
    const entrada = busca.querySelector('input');

    criarAutocompletar(entrada, {
        buscar: termo => buscarJson(configuracao.enderecoBuscaDeClientes, termo),
        descrever: cliente => ({ principal: cliente.nome, secundario: cliente.cpf }),
        textos: { ...textosDaBusca, vazio: configuracao.textoClienteNaoEncontrado },
        cadastro: { texto: configuracao.textoCadastrarCliente, endereco: configuracao.enderecoNovoCliente },
        aoEscolher: cliente => {
            mostrarEscolhido(cliente);
            aoEscolher();
        },
    });

    parte('trocar').addEventListener('click', buscarOutro);

    function mostrarEscolhido(cliente) {
        parte('id').value = cliente.id;
        parte('nome').value = cliente.nome;
        parte('cpf').value = cliente.cpf;
        parte('nome-exibido').textContent = cliente.nome;
        parte('cpf-exibido').textContent = cliente.cpf;
        mostrarAvatar(parte('avatar'), cliente.nome);
        busca.hidden = true;
        escolhido.hidden = false;
    }

    function buscarOutro() {
        if (!escolhido.hidden) {
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

function mostrarAvatar(avatar, nome) {
    avatar.textContent = iniciais(nome);
    avatar.className = `avatar avatar-tom-${tomDoAvatar(nome)}`;
}
