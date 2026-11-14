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
