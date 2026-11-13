# Sophia — controle de vendas

Aplicação web para o controle de vendas a clientes, em **ASP.NET Core MVC (.NET 8)**, **C#** e
**SQL Server**, com cadastro de clientes, produtos e vendas.

- [Especificação](#especificação)
- Script do banco: [`database/create-database.sql`](database/create-database.sql)
- Cenários executáveis (BDD): [`tests/Sophia.Specs/Funcionalidades`](tests/Sophia.Specs/Funcionalidades)

---

## Especificação

Esta seção descreve **o que** o sistema faz, na língua do negócio, antes de qualquer
detalhe de código. Cada regra aponta para o cenário executável que a comprova, em
`tests/Sophia.Specs/Funcionalidades`.

### 1. Objetivo

Aplicação web para o controle de vendas a clientes, com três cadastros: **clientes**,
**produtos** e **vendas**. A tela de vendas é a ferramenta de trabalho do operador e por isso
recebe o maior cuidado de usabilidade.

### 2. Atores

| Ator | Descrição |
|---|---|
| Operador | Pessoa que cadastra clientes e produtos e registra as vendas. |

### 3. Glossário

| Termo | Significado |
|---|---|
| CPF | Cadastro de Pessoa Física: 11 dígitos, os dois últimos são dígitos verificadores. |
| Lançamento / item | Uma linha da venda: produto, quantidade e valor unitário. |
| Subtotal | Quantidade × valor unitário de um item. |
| Total da venda | Soma dos subtotais dos itens. |

### 4. Requisitos funcionais

#### RF01 — Cadastro de clientes

Campos:

| Campo | Obrigatório | Regra |
|---|---|---|
| CPF | sim | válido pelos dígitos verificadores; único entre os clientes |
| Nome | sim | até 150 caracteres |
| Data de nascimento | não | não pode estar no futuro |
| E-mail | não | formato `nome@dominio.ext`, até 254 caracteres |
| Telefone | não | DDD + 8 ou 9 dígitos |

O CPF e o telefone podem ser digitados com ou sem máscara; são guardados só com os dígitos e
exibidos formatados (`529.982.247-25`, `(11) 98765-4321`).

Operações: listar com busca por CPF ou nome, cadastrar, alterar e excluir.

#### RF02 — Cadastro de produtos

| Campo | Obrigatório | Regra |
|---|---|---|
| Nome | sim | até 150 caracteres; único, sem diferenciar maiúsculas de minúsculas |
| Valor unitário | sim | maior que zero, no máximo duas casas decimais |

Operações: listar com busca por nome, cadastrar, alterar e excluir.

#### RF03 — Registro de vendas

A tela de venda:

1. sugere a **data de hoje**, que o operador pode alterar para uma data passada;
2. permite **selecionar o cliente digitando o CPF ou o nome**; ao escolher, mostra os dois;
3. permite **lançar vários produtos numa única operação**; em cada linha o operador escolhe o
   produto, confirma ou ajusta o **valor unitário** (sugerido pelo cadastro do produto) e
   informa a **quantidade**;
4. mostra o **subtotal de cada item** e o **total da venda**, atualizados enquanto digita;
5. ao confirmar, grava a venda e mostra o comprovante: número da venda, data, CPF e nome do
   cliente, itens com quantidades, preços e subtotais, e o total.

Operações: listar as vendas, registrar uma nova e consultar o detalhe de uma venda.

### 5. Regras de negócio

| Código | Regra | Mensagem ao usuário | Cenário |
|---|---|---|---|
| RN01 | CPF obrigatório | Informe o CPF. | Clientes › Recusar dados inválidos |
| RN02 | CPF precisa ser válido | CPF inválido. | Clientes › Recusar dados inválidos |
| RN03 | CPF não se repete, no cadastro nem na alteração | Já existe um cliente com este CPF. | Clientes › Não aceitar o mesmo CPF duas vezes |
| RN04 | Nome do cliente obrigatório | Informe o nome. | Clientes › Recusar dados inválidos |
| RN05 | Nascimento não pode estar no futuro | A data de nascimento não pode estar no futuro. | Clientes › Recusar dados inválidos |
| RN06 | E-mail, se informado, válido | E-mail inválido. | Clientes › Recusar dados inválidos |
| RN07 | Telefone, se informado, com DDD | Informe o telefone com DDD. | Clientes › Recusar dados inválidos |
| RN08 | Cliente com venda não pode ser excluído | Este cliente possui vendas e não pode ser excluído. | Clientes › Não excluir cliente que já comprou |
| RN09 | Nome do produto obrigatório | Informe o nome. | Produtos › Recusar dados inválidos |
| RN10 | Nome do produto não se repete | Já existe um produto com este nome. | Produtos › Não aceitar dois produtos com o mesmo nome |
| RN11 | Valor unitário obrigatório | Informe o valor unitário. | Produtos › Recusar dados inválidos |
| RN12 | Valor unitário maior que zero | O valor unitário deve ser maior que zero. | Produtos › Recusar dados inválidos |
| RN13 | Valor unitário com até duas casas | O valor unitário aceita no máximo duas casas decimais. | Produtos › Recusar dados inválidos |
| RN14 | Produto vendido não pode ser excluído | Este produto já foi vendido e não pode ser excluído. | Produtos › Não excluir produto que já foi vendido |
| RN15 | Venda exige cliente | Selecione o cliente. | Vendas › Não registrar venda sem cliente |
| RN16 | Data da venda não pode estar no futuro | A data da venda não pode estar no futuro. | Vendas › Não registrar venda com data futura |
| RN17 | Venda exige ao menos um item | Lance ao menos um produto na venda. | Vendas › Não registrar venda sem itens |
| RN18 | Quantidade maior que zero | A quantidade deve ser maior que zero. | Vendas › Recusar item inválido |
| RN19 | Valor unitário do item maior que zero | O valor unitário deve ser maior que zero. | Vendas › Recusar item inválido |
| RN20 | Mesmo produto não se repete na venda | Este produto já foi lançado nesta venda; ajuste a quantidade na linha anterior. | Vendas › Não lançar o mesmo produto duas vezes |
| RN21 | O valor do item é o praticado na venda: mudar o preço do produto depois não altera vendas já feitas | — | Vendas › O valor lançado na venda não muda quando o preço do produto muda |
| RN22 | Subtotal e total são sempre calculados pelo sistema, nunca aceitos prontos da tela | — | Vendas › Registrar uma venda com vários produtos |

### 6. Requisitos não funcionais

| Código | Requisito |
|---|---|
| RNF01 | ASP.NET Core MVC (.NET 8) com C#. |
| RNF02 | SQL Server, acesso via Entity Framework Core. |
| RNF03 | Banco com chaves primárias e estrangeiras, índices, campos obrigatórios e normalização; o script de criação é entregue em `database/create-database.sql`. |
| RNF04 | Interface simples e moderna, em português do Brasil (datas `dd/mm/aaaa`, valores `R$ 1.234,56`), utilizável também em telas estreitas. |
| RNF05 | Validação em dois níveis: no navegador, para resposta imediata, e no servidor, que é a fonte da verdade. |
| RNF06 | Erros inesperados mostram uma página amigável, sem detalhes técnicos. |
| RNF07 | Executável com Docker (`docker compose up`), sem instalar .NET ou SQL Server na máquina. |

### 7. Modelo de dados

```
Clientes (1) ───< Vendas (1) ───< VendaItens >─── (1) Produtos
```

| Tabela | Coluna | Tipo | Restrição |
|---|---|---|---|
| Clientes | Id | int identity | PK |
| | Cpf | char(11) | obrigatório, único (`UQ_Clientes_Cpf`) |
| | Nome | nvarchar(150) | obrigatório, indexado (`IX_Clientes_Nome`) |
| | DataNascimento | date | opcional |
| | Email | nvarchar(254) | opcional |
| | Telefone | varchar(11) | opcional, só dígitos |
| Produtos | Id | int identity | PK |
| | Nome | nvarchar(150) | obrigatório, único (`UQ_Produtos_Nome`) |
| | ValorUnitario | decimal(18,2) | obrigatório, `> 0` |
| Vendas | Id | int identity | PK — é o número da venda |
| | Data | date | obrigatório, indexado |
| | ClienteId | int | FK → Clientes, sem exclusão em cascata, indexado |
| VendaItens | Id | int identity | PK |
| | VendaId | int | FK → Vendas, exclusão em cascata |
| | ProdutoId | int | FK → Produtos, sem exclusão em cascata |
| | Quantidade | int | obrigatório, `> 0` |
| | ValorUnitario | decimal(18,2) | obrigatório, `> 0` |
| | | | único por (`VendaId`, `ProdutoId`) |

Subtotal e total não são gravados: são derivados dos itens. Guardar o valor calculado
criaria duas fontes para o mesmo dado.

### 8. Rastreabilidade

| Requisito | Especificação executável | Testes unitários | Integração / tela |
|---|---|---|---|
| RF01 | `CadastroDeClientes.feature` | `Sophia.Domain.Tests` (Cpf, Cliente), `Sophia.Application.Tests` | `Sophia.Integration.Tests`, `Sophia.E2E` |
| RF02 | `CadastroDeProdutos.feature` | `Sophia.Domain.Tests` (Produto), `Sophia.Application.Tests` | `Sophia.Integration.Tests`, `Sophia.E2E` |
| RF03 | `RegistroDeVendas.feature` | `Sophia.Domain.Tests` (Venda), `Sophia.Application.Tests` | `Sophia.Integration.Tests`, `Sophia.E2E` |
