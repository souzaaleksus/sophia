Funcionalidade: Cadastro de clientes
  Para vender a quem a empresa já conhece
  Como operador
  Quero manter os dados dos clientes, sem repetir o mesmo CPF

  Cenário: Cadastrar um cliente com todos os dados
    Quando eu cadastrar o cliente
      | Cpf            | Nome         | DataNascimento | Email                | Telefone        |
      | 529.982.247-25 | Maria Santos | 15/03/1990     | maria@exemplo.com.br | (11) 98765-4321 |
    Então o cadastro é aceito
    E o cliente "Maria Santos" aparece na lista com o CPF "529.982.247-25"

  Cenário: Cadastrar só com os dados obrigatórios
    Quando eu cadastrar o cliente
      | Cpf         | Nome      | DataNascimento | Email | Telefone |
      | 52998224725 | João Lima |                |       |          |
    Então o cadastro é aceito
    E o cliente "João Lima" aparece na lista com o CPF "529.982.247-25"

  Cenário: Não aceitar o mesmo CPF duas vezes
    Dado que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    Quando eu cadastrar o cliente
      | Cpf            | Nome        | DataNascimento | Email | Telefone |
      | 529.982.247-25 | Maria Souza |                |       |          |
    Então o cadastro é recusado com a mensagem "Já existe um cliente com este CPF." no campo "Cpf"

  Esquema do Cenário: Recusar dados inválidos
    Quando eu cadastrar o cliente
      | Cpf   | Nome   | DataNascimento   | Email   | Telefone   |
      | <Cpf> | <Nome> | <DataNascimento> | <Email> | <Telefone> |
    Então o cadastro é recusado com a mensagem "<Mensagem>" no campo "<Campo>"

    Exemplos:
      | Cpf            | Nome | DataNascimento | Email | Telefone | Campo          | Mensagem                                       |
      |                | Ana  |                |       |          | Cpf            | Informe o CPF.                                 |
      | 111.111.111-11 | Ana  |                |       |          | Cpf            | CPF inválido.                                  |
      | 529.982.247-24 | Ana  |                |       |          | Cpf            | CPF inválido.                                  |
      | 529.982.247-25 |      |                |       |          | Nome           | Informe o nome.                                |
      | 529.982.247-25 | Ana  | 01/01/2999     |       |          | DataNascimento | A data de nascimento não pode estar no futuro. |
      | 529.982.247-25 | Ana  |                | ana@  |          | Email          | E-mail inválido.                               |
      | 529.982.247-25 | Ana  |                |       | 1234     | Telefone       | Informe o telefone com DDD.                    |

  Cenário: Alterar os dados de um cliente
    Dado que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    Quando eu alterar o nome do cliente "Maria Santos" para "Maria Santos Silva"
    Então a alteração é aceita
    E o cliente "Maria Santos Silva" aparece na lista com o CPF "529.982.247-25"

  Cenário: Não deixar a alteração repetir o CPF de outro cliente
    Dado que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    E que existe o cliente "João Lima" com o CPF "111.444.777-35"
    Quando eu alterar o CPF do cliente "João Lima" para "529.982.247-25"
    Então a alteração é recusada com a mensagem "Já existe um cliente com este CPF." no campo "Cpf"

  Cenário: Encontrar o cliente pelo CPF ou pelo nome
    Dado que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    E que existe o cliente "João Lima" com o CPF "111.444.777-35"
    Quando eu buscar clientes por "529.982"
    Então encontro somente o cliente "Maria Santos"
    Quando eu buscar clientes por "lima"
    Então encontro somente o cliente "João Lima"

  Cenário: Excluir um cliente que nunca comprou
    Dado que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    Quando eu excluir o cliente "Maria Santos"
    Então a exclusão é aceita
    E a lista de clientes fica vazia

  Cenário: Não excluir cliente que já comprou
    Dado que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    E que existe o produto "Caneta azul" de R$ 2,50
    E que o cliente "Maria Santos" comprou 1 "Caneta azul"
    Quando eu excluir o cliente "Maria Santos"
    Então a exclusão é recusada com a mensagem "Este cliente possui vendas e não pode ser excluído."
