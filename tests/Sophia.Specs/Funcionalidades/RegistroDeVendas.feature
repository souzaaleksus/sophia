Funcionalidade: Registro de vendas
  Para controlar o que foi vendido a cada cliente
  Como operador
  Quero lançar vários produtos numa única venda e ver os totais por item e da venda

  Contexto:
    Dado que hoje é 13/11/2026
    E que existe o cliente "Maria Santos" com o CPF "529.982.247-25"
    E que existe o produto "Caneta azul" de R$ 2,50
    E que existe o produto "Caderno 96 folhas" de R$ 18,90

  Cenário: A nova venda sugere a data de hoje
    Quando eu iniciar uma nova venda
    Então a data sugerida é 13/11/2026

  Cenário: Registrar uma venda com vários produtos
    Quando eu registrar uma venda para o cliente "Maria Santos" em 13/11/2026 com os itens
      | Produto           | Quantidade | ValorUnitario |
      | Caneta azul       | 10         | 2,50          |
      | Caderno 96 folhas | 2          | 18,90         |
    Então a venda é registrada
    E a venda mostra o CPF "529.982.247-25" e o nome "Maria Santos"
    E a venda tem os itens
      | Produto           | Quantidade | ValorUnitario | Subtotal |
      | Caneta azul       | 10         | 2,50          | 25,00    |
      | Caderno 96 folhas | 2          | 18,90         | 37,80    |
    E o total da venda é "62,80"

  Cenário: Enviar a mesma venda duas vezes registra uma só
    Quando eu enviar duas vezes a mesma venda para o cliente "Maria Santos" em 13/11/2026 com os itens
      | Produto     | Quantidade | ValorUnitario |
      | Caneta azul | 10         | 2,50          |
    Então os dois envios apontam para a mesma venda
    E existe 1 venda registrada

  Cenário: Duas vendas iguais feitas uma depois da outra são registradas as duas
    Quando eu registrar uma venda para o cliente "Maria Santos" em 13/11/2026 com os itens
      | Produto     | Quantidade | ValorUnitario |
      | Caneta azul | 10         | 2,50          |
    E eu registrar uma venda para o cliente "Maria Santos" em 13/11/2026 com os itens
      | Produto     | Quantidade | ValorUnitario |
      | Caneta azul | 10         | 2,50          |
    Então existem 2 vendas registradas

  Cenário: O valor lançado na venda não muda quando o preço do produto muda
    Dado que o cliente "Maria Santos" comprou 4 "Caneta azul"
    Quando eu alterar o valor do produto "Caneta azul" para "3,00"
    Então a última venda continua com o total "10,00"

  Cenário: Não registrar venda sem itens
    Quando eu registrar uma venda para o cliente "Maria Santos" em 13/11/2026 sem itens
    Então a venda é recusada com a mensagem "Lance ao menos um produto na venda." no campo "Itens"

  Cenário: Não registrar venda sem cliente
    Quando eu registrar uma venda sem cliente em 13/11/2026 com os itens
      | Produto     | Quantidade | ValorUnitario |
      | Caneta azul | 1          | 2,50          |
    Então a venda é recusada com a mensagem "Selecione o cliente." no campo "ClienteId"

  Cenário: Não registrar venda com data futura
    Quando eu registrar uma venda para o cliente "Maria Santos" em 14/11/2026 com os itens
      | Produto     | Quantidade | ValorUnitario |
      | Caneta azul | 1          | 2,50          |
    Então a venda é recusada com a mensagem "A data da venda não pode estar no futuro." no campo "Data"

  Esquema do Cenário: Recusar item inválido
    Quando eu registrar uma venda para o cliente "Maria Santos" em 13/11/2026 com os itens
      | Produto     | Quantidade   | ValorUnitario |
      | Caneta azul | <Quantidade> | <Valor>       |
    Então a venda é recusada com a mensagem "<Mensagem>" no campo "<Campo>"

    Exemplos:
      | Quantidade | Valor | Campo                  | Mensagem                                  |
      | 0          | 2,50  | Itens[0].Quantidade    | A quantidade deve ser maior que zero.     |
      | 1          | 0,00  | Itens[0].ValorUnitario | O valor unitário deve ser maior que zero. |

  Cenário: Não lançar o mesmo produto duas vezes
    Quando eu registrar uma venda para o cliente "Maria Santos" em 13/11/2026 com os itens
      | Produto     | Quantidade | ValorUnitario |
      | Caneta azul | 1          | 2,50          |
      | Caneta azul | 2          | 2,50          |
    Então a venda é recusada com a mensagem "Este produto já foi lançado nesta venda; ajuste a quantidade na linha anterior." no campo "Itens[1].ProdutoId"
