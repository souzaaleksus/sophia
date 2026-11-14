Funcionalidade: Consulta de vendas
  Para achar uma venda sem carregar o histórico inteiro
  Como operador
  Quero ver as vendas em páginas, das mais recentes para as mais antigas

  Contexto:
    Dado que hoje é 13/11/2026
    E que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    E que existe o produto "Caneta azul" de R$ 2,50
    E que foram registradas 60 vendas, uma por dia, até 13/11/2026

  Cenário: A lista de vendas mostra 25 por página, das mais recentes para as mais antigas
    Quando eu consultar a página 1 das vendas
    Então vejo 25 vendas, de 13/11/2026 a 20/10/2026
    E estou na página 1 de 3, com as vendas 1 a 25 de 60
    E não há página anterior
    E há página seguinte

  Cenário: A última página traz as vendas mais antigas que sobraram
    Quando eu consultar a página 3 das vendas
    Então vejo 10 vendas, de 24/09/2026 a 15/09/2026
    E estou na página 3 de 3, com as vendas 51 a 60 de 60
    E há página anterior
    E não há página seguinte

  Esquema do Cenário: Página fora dos limites mostra a página válida mais próxima
    Quando eu consultar a página <Pedida> das vendas
    Então estou na página <Mostrada> de 3, com as vendas <Primeira> a <Ultima> de 60

    Exemplos:
      | Pedida | Mostrada | Primeira | Ultima |
      | 0      | 1        | 1        | 25     |
      | -5     | 1        | 1        | 25     |
      | 99     | 3        | 51       | 60     |
