Funcionalidade: Painel de vendas
  Para acompanhar o movimento do dia e do mês
  Como operador
  Quero ver os totais logo ao abrir o sistema

  Contexto:
    Dado que hoje é 13/11/2026
    E que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    E que existe o produto "Caneta azul" de R$ 2,50
    E que existe o produto "Caderno 96 folhas" de R$ 18,90

  Cenário: Painel antes da primeira venda
    Quando eu abrir o painel
    Então vejo 0 vendas hoje somando "0,00"
    E vejo 0 vendas no mês somando "0,00" com ticket médio de "0,00"
    E não há últimas vendas
    E os últimos 7 dias aparecem zerados
    E não há produtos mais vendidos no mês

  Cenário: Totais do dia e do mês
    Dado que foram registradas as vendas
      | Data       | Cliente      | Produto           | Quantidade | ValorUnitario |
      | 31/10/2026 | Maria Santos | Caneta azul       | 4          | 2,50          |
      | 02/11/2026 | Maria Santos | Caderno 96 folhas | 2          | 18,90         |
      | 13/11/2026 | Maria Santos | Caneta azul       | 10         | 2,50          |
      | 13/11/2026 | Maria Santos | Caderno 96 folhas | 1          | 18,90         |
    Quando eu abrir o painel
    Então vejo 2 vendas hoje somando "43,90"
    E vejo 3 vendas no mês somando "81,70" com ticket médio de "27,23"
    E as últimas vendas começam pela mais recente, de 13/11/2026

  Cenário: Total de cada um dos últimos 7 dias, inclusive os dias sem venda
    Dado que foram registradas as vendas
      | Data       | Cliente      | Produto           | Quantidade | ValorUnitario |
      | 06/11/2026 | Maria Santos | Caneta azul       | 1          | 2,50          |
      | 07/11/2026 | Maria Santos | Caneta azul       | 4          | 2,50          |
      | 10/11/2026 | Maria Santos | Caderno 96 folhas | 1          | 18,90         |
      | 10/11/2026 | Maria Santos | Caneta azul       | 2          | 2,50          |
      | 13/11/2026 | Maria Santos | Caneta azul       | 10         | 2,50          |
    Quando eu abrir o painel
    Então os últimos 7 dias mostram
      | Dia        | Vendas | Total |
      | 07/11/2026 | 1      | 10,00 |
      | 08/11/2026 | 0      | 0,00  |
      | 09/11/2026 | 0      | 0,00  |
      | 10/11/2026 | 2      | 23,90 |
      | 11/11/2026 | 0      | 0,00  |
      | 12/11/2026 | 0      | 0,00  |
      | 13/11/2026 | 1      | 25,00 |

  Cenário: Os 5 produtos mais vendidos do mês, por quantidade
    Dado que existe o produto "Lápis preto" de R$ 1,50
    E que existe o produto "Borracha branca" de R$ 1,50
    E que existe o produto "Régua 30 cm" de R$ 4,00
    E que existe o produto "Cola bastão" de R$ 6,00
    E que foram registradas as vendas
      | Data       | Cliente      | Produto           | Quantidade | ValorUnitario |
      | 31/10/2026 | Maria Santos | Cola bastão       | 50         | 6,00          |
      | 02/11/2026 | Maria Santos | Caneta azul       | 6          | 2,50          |
      | 05/11/2026 | Maria Santos | Caneta azul       | 4          | 2,50          |
      | 05/11/2026 | Maria Santos | Lápis preto       | 3          | 1,50          |
      | 08/11/2026 | Maria Santos | Caderno 96 folhas | 3          | 18,90         |
      | 09/11/2026 | Maria Santos | Borracha branca   | 3          | 1,50          |
      | 12/11/2026 | Maria Santos | Régua 30 cm       | 2          | 4,00          |
      | 13/11/2026 | Maria Santos | Cola bastão       | 1          | 6,00          |
    Quando eu abrir o painel
    Então os mais vendidos do mês são
      | Produto           | Quantidade | Total |
      | Caneta azul       | 10         | 25,00 |
      | Caderno 96 folhas | 3          | 56,70 |
      | Borracha branca   | 3          | 4,50  |
      | Lápis preto       | 3          | 4,50  |
      | Régua 30 cm       | 2          | 8,00  |
