Funcionalidade: Consulta de vendas
  Para achar uma venda sem carregar o histórico inteiro
  Como operador
  Quero ver as vendas em páginas, filtrar por período e ordenar pela data ou pelo total

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

  Cenário: Filtrar as vendas por período, com as duas datas inclusive
    Quando eu consultar as vendas de 01/11/2026 a 10/11/2026
    Então vejo 10 vendas, de 10/11/2026 a 01/11/2026
    E estou na página 1 de 1, com as vendas 1 a 10 de 10

  Cenário: Período digitado ao contrário é lido na ordem certa
    Quando eu consultar as vendas de 10/11/2026 a 01/11/2026
    Então vejo 10 vendas, de 10/11/2026 a 01/11/2026
    E estou na página 1 de 1, com as vendas 1 a 10 de 10

  Cenário: Período só com a data inicial vai até a venda mais recente
    Quando eu consultar as vendas a partir de 05/11/2026
    Então vejo 9 vendas, de 13/11/2026 a 05/11/2026

  Cenário: Período só com a data final vai desde a venda mais antiga
    Quando eu consultar as vendas até 17/09/2026
    Então vejo 3 vendas, de 17/09/2026 a 15/09/2026

  Cenário: Ordenar das mais antigas para as mais recentes
    Quando eu consultar a página 1 das vendas ordenada por "data"
    Então vejo 25 vendas, de 15/09/2026 a 09/10/2026
    E a lista de vendas está ordenada por "data"

  Esquema do Cenário: Ordenar pelo total da venda
    Dado que em 05/11/2026 a cliente "Maria Santos" comprou 8 "Caneta azul"
    Quando eu consultar a página 1 das vendas ordenada por "<Pedida>"
    Então a primeira venda é a de <Data> com total de R$ <Total>
    E a lista de vendas está ordenada por "<Valendo>"

    Exemplos:
      | Pedida  | Data       | Total | Valendo |
      | -total  | 05/11/2026 | 20,00 | -total  |
      | total   | 15/09/2026 | 2,50  | total   |
      | cliente | 13/11/2026 | 2,50  | -data   |
