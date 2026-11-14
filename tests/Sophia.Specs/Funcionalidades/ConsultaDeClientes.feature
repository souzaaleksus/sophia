Funcionalidade: Consulta de clientes
  Para achar um cliente sem rolar a carteira inteira
  Como operador
  Quero ver os clientes em páginas, buscar pelo CPF ou pelo nome e ordenar pela coluna

  Contexto:
    Dado que estão cadastrados 30 clientes, de "Cliente 01" a "Cliente 30", com os CPFs na ordem inversa à dos nomes

  Cenário: A lista de clientes mostra 25 por página, em ordem alfabética
    Quando eu consultar a página 1 dos clientes
    Então vejo 25 clientes, de "Cliente 01" a "Cliente 25"
    E estou na página 1 de 2, com os clientes 1 a 25 de 30
    E a lista de clientes está ordenada por "nome"

  Cenário: A segunda página traz os clientes que sobraram
    Quando eu consultar a página 2 dos clientes
    Então vejo 5 clientes, de "Cliente 26" a "Cliente 30"
    E estou na página 2 de 2, com os clientes 26 a 30 de 30

  Esquema do Cenário: Ordenar a lista pela coluna escolhida
    Quando eu consultar a página 1 dos clientes ordenada por "<Pedida>"
    Então vejo 25 clientes, de "<Primeiro>" a "<Ultimo>"
    E a lista de clientes está ordenada por "<Valendo>"

    Exemplos:
      | Pedida   | Primeiro   | Ultimo     | Valendo |
      | nome     | Cliente 01 | Cliente 25 | nome    |
      | -nome    | Cliente 30 | Cliente 06 | -nome   |
      | cpf      | Cliente 30 | Cliente 06 | cpf     |
      | -cpf     | Cliente 01 | Cliente 25 | -cpf    |
      | telefone | Cliente 01 | Cliente 25 | nome    |

  Cenário: A busca pelo nome filtra antes de dividir em páginas
    Quando eu buscar "cliente 1" na lista de clientes
    Então vejo 10 clientes, de "Cliente 10" a "Cliente 19"
    E estou na página 1 de 1, com os clientes 1 a 10 de 10

  Cenário: A busca pelo CPF encontra o dono do CPF
    Quando eu buscar na lista de clientes o CPF de "Cliente 07"
    Então vejo 1 cliente, de "Cliente 07" a "Cliente 07"

  Esquema do Cenário: A busca pelo nome não diferencia acentos nem maiúsculas
    Dado que existe o cliente "João Lima" com o CPF "529.982.247-25"
    Quando eu buscar "<Termo>" na lista de clientes
    Então vejo 1 cliente, de "João Lima" a "João Lima"

    Exemplos:
      | Termo |
      | joao  |
      | JOÃO  |
      | Joao  |

  Esquema do Cenário: Busca só com pontuação não traz a carteira inteira
    Quando eu buscar "<Termo>" na lista de clientes
    Então nenhum cliente é encontrado

    Exemplos:
      | Termo |
      | -     |
      | .-    |
      | ...   |

  Esquema do Cenário: Página fora dos limites mostra a página válida mais próxima
    Quando eu consultar a página <Pedida> dos clientes
    Então estou na página <Mostrada> de 2, com os clientes <Primeiro> a <Ultimo> de 30

    Exemplos:
      | Pedida | Mostrada | Primeiro | Ultimo |
      | 0      | 1        | 1        | 25     |
      | -2     | 1        | 1        | 25     |
      | 99     | 2        | 26       | 30     |
