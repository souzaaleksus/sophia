Funcionalidade: Consulta de produtos
  Para achar um produto sem rolar o catálogo inteiro
  Como operador
  Quero ver os produtos em páginas, buscar pelo nome e ordenar pelo nome ou pelo valor

  Contexto:
    Dado que estão cadastrados 30 produtos, de "Produto 01" a "Produto 30", com o valor caindo de R$ 30,00 a R$ 1,00

  Cenário: A lista de produtos mostra 25 por página, em ordem alfabética
    Quando eu consultar a página 1 dos produtos
    Então vejo 25 produtos, de "Produto 01" a "Produto 25"
    E estou na página 1 de 2, com os produtos 1 a 25 de 30
    E a lista de produtos está ordenada por "nome"

  Esquema do Cenário: Ordenar a lista pela coluna escolhida
    Quando eu consultar a página 1 dos produtos ordenada por "<Pedida>"
    Então vejo 25 produtos, de "<Primeiro>" a "<Ultimo>"
    E o primeiro produto custa "<Valor>"
    E a lista de produtos está ordenada por "<Valendo>"

    Exemplos:
      | Pedida | Primeiro   | Ultimo     | Valor | Valendo |
      | nome   | Produto 01 | Produto 25 | 30,00 | nome    |
      | -nome  | Produto 30 | Produto 06 | 1,00  | -nome   |
      | valor  | Produto 30 | Produto 06 | 1,00  | valor   |
      | -valor | Produto 01 | Produto 25 | 30,00 | -valor  |
      | preco  | Produto 01 | Produto 25 | 30,00 | nome    |

  Cenário: A busca pelo nome filtra antes de dividir em páginas
    Quando eu buscar "produto 2" na lista de produtos ordenada por "-valor"
    Então vejo 10 produtos, de "Produto 20" a "Produto 29"
    E estou na página 1 de 1, com os produtos 1 a 10 de 10

  Cenário: A busca pelo nome não diferencia acentos nem maiúsculas
    Dado que existe o produto "Açúcar cristal" de R$ 5,00
    Quando eu buscar "acucar" na lista de produtos
    Então vejo 1 produto, de "Açúcar cristal" a "Açúcar cristal"

  Esquema do Cenário: Página fora dos limites mostra a página válida mais próxima
    Quando eu consultar a página <Pedida> dos produtos
    Então estou na página <Mostrada> de 2, com os produtos <Primeiro> a <Ultimo> de 30

    Exemplos:
      | Pedida | Mostrada | Primeiro | Ultimo |
      | 0      | 1        | 1        | 25     |
      | 99     | 2        | 26       | 30     |
