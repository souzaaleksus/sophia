Funcionalidade: Cadastro de produtos
  Para lançar nas vendas o que a empresa comercializa
  Como operador
  Quero manter os produtos com nome único e valor unitário

  Cenário: Cadastrar um produto
    Quando eu cadastrar o produto "Caderno 96 folhas" com valor unitário de "18,90"
    Então o cadastro é aceito
    E o produto "Caderno 96 folhas" aparece na lista com o valor "18,90"

  Cenário: Não aceitar dois produtos com o mesmo nome
    Dado que existe o produto "Caneta azul" de R$ 2,50
    Quando eu cadastrar o produto "caneta azul" com valor unitário de "3,00"
    Então o cadastro é recusado com a mensagem "Já existe um produto com este nome." no campo "Nome"

  Esquema do Cenário: Recusar dados inválidos
    Quando eu cadastrar o produto "<Nome>" com valor unitário de "<Valor>"
    Então o cadastro é recusado com a mensagem "<Mensagem>" no campo "<Campo>"

    Exemplos:
      | Nome  | Valor | Campo         | Mensagem                                               |
      |       | 10,00 | Nome          | Informe o nome.                                        |
      | Lápis |       | ValorUnitario | Informe o valor unitário.                              |
      | Lápis | 0,00  | ValorUnitario | O valor unitário deve ser maior que zero.              |
      | Lápis | -1,00 | ValorUnitario | O valor unitário deve ser maior que zero.              |
      | Lápis | 1,999 | ValorUnitario | O valor unitário aceita no máximo duas casas decimais. |

  Cenário: Alterar o valor de um produto
    Dado que existe o produto "Caneta azul" de R$ 2,50
    Quando eu alterar o valor do produto "Caneta azul" para "2,75"
    Então a alteração é aceita
    E o produto "Caneta azul" aparece na lista com o valor "2,75"

  Cenário: Não excluir produto que já foi vendido
    Dado que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    E que existe o produto "Caneta azul" de R$ 2,50
    E que o cliente "Maria Santos" comprou 1 "Caneta azul"
    Quando eu excluir o produto "Caneta azul"
    Então a exclusão é recusada com a mensagem "Este produto já foi vendido e não pode ser excluído."
