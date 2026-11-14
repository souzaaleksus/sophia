# Sophia — Controle de Venda

Aplicação web para o controle de vendas a clientes, em **ASP.NET Core MVC (.NET 8)**, **C#** e
**SQL Server**, com cadastro de clientes, produtos e vendas.

- [Especificação](#especificação)
- [Como executar](#como-executar)
- [Guia do usuário](docs/guia-do-usuario.md): como operar o sistema, com as telas
- [Testes](#testes)
- [Arquitetura](#arquitetura)
- [Decisões](#decisões)
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

A tela é pensada para o balcão e funciona inteira pelo teclado: **F2** busca o cliente, **F4**
lança um produto, **Enter** na quantidade abre a próxima linha e **Ctrl+Enter** finaliza.

#### RF04 — Painel

A página inicial resume o movimento: quantidade e total das vendas de hoje, quantidade, total e
ticket médio do mês corrente e as cinco vendas mais recentes, com atalho para uma nova venda.

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
| RN23 | O mês do painel vai do dia 1º até hoje | — | Painel › Totais do dia e do mês |
| RN24 | Ticket médio = total do mês ÷ número de vendas do mês, arredondado aos centavos; sem vendas, é zero | — | Painel › Totais do dia e do mês |
| RN25 | Venda enviada duas vezes é registrada uma vez só: o segundo envio devolve a venda já gravada | — | Vendas › Enviar a mesma venda duas vezes registra uma só |
| RN26 | O painel mostra o total de cada um dos últimos 7 dias, até hoje, inclusive os dias sem venda | — | Painel › Total de cada um dos últimos 7 dias, inclusive os dias sem venda |
| RN27 | O painel mostra os 5 produtos mais vendidos do mês por quantidade; no empate, vem o de maior total e depois a ordem alfabética | — | Painel › Os 5 produtos mais vendidos do mês, por quantidade |
| RN28 | A lista de vendas mostra 25 vendas por página, das mais recentes para as mais antigas; página pedida antes da primeira mostra a primeira, e depois da última mostra a última | — | Consulta de vendas › A lista de vendas mostra 25 por página, das mais recentes para as mais antigas |

### 6. Requisitos não funcionais

| Código | Requisito |
|---|---|
| RNF01 | ASP.NET Core MVC (.NET 8) com C#. |
| RNF02 | SQL Server (Express), acesso via Entity Framework Core. |
| RNF03 | Banco com chaves primárias e estrangeiras, índices, campos obrigatórios e normalização; o script de criação é entregue em `database/create-database.sql`. |
| RNF04 | Interface simples e moderna, em português do Brasil (datas `dd/mm/aaaa`, valores `R$ 1.234,56`), utilizável também em telas estreitas, com tema claro, escuro ou automático. |
| RNF05 | Validação em dois níveis: no navegador, para resposta imediata, e no servidor, que é a fonte da verdade. |
| RNF06 | Erros inesperados mostram uma página amigável, sem detalhes técnicos. |
| RNF07 | Executável com Docker (`docker compose up`), sem instalar .NET ou SQL Server na máquina. |

### 7. Stack

| Camada | Tecnologia | Por quê |
|---|---|---|
| Linguagem e plataforma | C# 12 · .NET 8 (LTS) | versão com suporte de longo prazo |
| Web | ASP.NET Core MVC · Razor | pedido no enunciado; páginas renderizadas no servidor |
| Interface | Bootstrap 5.3 · CSS próprio · JavaScript puro (módulos ES) | leve, sem etapa de build no front-end; tema escuro nativo |
| Tipografia e ícones | Manrope (Google Fonts) · ícones Lucide (licença ISC) em sprite SVG | legibilidade no balcão, algarismos tabulares nos valores |
| Acesso a dados | Entity Framework Core 8 | mapeamento explícito via Fluent API, sem migrations |
| Banco de dados | SQL Server 2019 Express (local) · SQL Server 2022 Express (Docker) | edição gratuita do SQL Server |
| Testes de unidade | xUnit · FluentAssertions | domínio e serviços isolados |
| Especificação executável (BDD) | Reqnroll · Gherkin em português | os cenários deste documento rodam como testes |
| Testes de integração | xUnit · SQL Server real | o banco é criado pelo próprio script entregue |
| Testes de tela (E2E) | Playwright para .NET | navegador de verdade operando a aplicação |
| Contêineres | Docker · Docker Compose | sobe banco e aplicação com um comando |
| Integração contínua | GitHub Actions | compila e roda os testes a cada push |

### 8. Modelo de dados

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
| | ChaveDeRegistro | uniqueidentifier | opcional; única entre as vendas que a têm (`UQ_Vendas_ChaveDeRegistro`, índice filtrado `WHERE ChaveDeRegistro IS NOT NULL`) |
| VendaItens | Id | int identity | PK |
| | VendaId | int | FK → Vendas, exclusão em cascata |
| | ProdutoId | int | FK → Produtos, sem exclusão em cascata |
| | Quantidade | int | obrigatório, `> 0` |
| | ValorUnitario | decimal(18,2) | obrigatório, `> 0` |
| | | | único por (`VendaId`, `ProdutoId`) |

Subtotal e total não são gravados: são derivados dos itens. Guardar o valor calculado
criaria duas fontes para o mesmo dado.

A `ChaveDeRegistro` é gerada quando a tela de nova venda abre e volta com o formulário. Se o
mesmo formulário chega duas vezes, o índice único impede a segunda gravação e o sistema
devolve a venda já registrada. Em banco criado por versão anterior do script, rodar o script
de novo acrescenta a coluna e o índice sem apagar nada.

### 9. Rastreabilidade

| Requisito | Especificação executável | Testes unitários | Integração / tela |
|---|---|---|---|
| RF01 | `CadastroDeClientes.feature` | `Sophia.Domain.Tests` (Cpf, Cliente), `Sophia.Application.Tests` | `Sophia.Integration.Tests`, `Sophia.E2E` |
| RF02 | `CadastroDeProdutos.feature` | `Sophia.Domain.Tests` (Produto), `Sophia.Application.Tests` | `Sophia.Integration.Tests`, `Sophia.E2E` |
| RF03 | `RegistroDeVendas.feature` | `Sophia.Domain.Tests` (Venda), `Sophia.Application.Tests` | `Sophia.Integration.Tests`, `Sophia.E2E` |
| RF04 | `PainelDeVendas.feature` | `Sophia.Application.Tests` (PainelServico, ResumoDoPeriodo) | `Sophia.Integration.Tests`, `Sophia.E2E` |
| RN25 | `RegistroDeVendas.feature` › Enviar a mesma venda duas vezes registra uma só | `Sophia.Domain.Tests` (Venda), `Sophia.Application.Tests` (VendaServico, inclusive a corrida entre dois envios) | `Sophia.Integration.Tests` (índice único filtrado, corrida no SQL Server, atualização do banco pelo script) |
| RN26 | `PainelDeVendas.feature` › Total de cada um dos últimos 7 dias | `Sophia.Application.Tests` (PainelServico) | `Sophia.Integration.Tests` (totais por dia agregados no SQL Server) |
| RN27 | `PainelDeVendas.feature` › Os 5 produtos mais vendidos do mês | `Sophia.Application.Tests` (PainelServico) | `Sophia.Integration.Tests` (mais vendidos agregados no SQL Server) |
| RN28 | `ConsultaDeVendas.feature` | `Sophia.Application.Tests` (Pagina, VendaServico: tamanho, ordem e limites de página) | `Sophia.Integration.Tests` (contagem e recorte com `OFFSET`/`FETCH` no SQL Server) |

---

## Como executar

Roteiro do zero até a primeira venda. Escolha **um** dos dois caminhos: Docker (nada para
instalar além do Docker) ou local (.NET 8 e SQL Server Express na máquina).

### Passo 0 — Baixar o código

```bash
git clone https://github.com/souzaaleksus/sophia.git
cd sophia
```

### Caminho A — Docker

Pré-requisito: Docker com o plugin Compose (Docker Desktop no Windows/macOS ou Docker Engine
no Linux/WSL).

1. Suba a aplicação e o banco:

   ```bash
   docker compose up -d --build
   ```

   O Compose sobe três serviços, nesta ordem:

   1. `banco`: SQL Server 2022 Express;
   2. `criar-banco`: executa `database/create-database.sql` e termina;
   3. `web`: a aplicação, que só sobe depois que o banco foi criado.

2. Acompanhe até a aplicação ficar pronta (a primeira vez leva alguns minutos, por causa do
   download das imagens):

   ```bash
   docker compose ps
   docker compose logs -f web
   ```

   Quando aparecer `Now listening on: http://[::]:8080`, ela está no ar. Para sair dos logs,
   use `Ctrl+C` (a aplicação continua rodando).

3. Abra http://localhost:8080.

4. Para parar: `docker compose down`. Os dados ficam no volume `dados-do-banco` e voltam na
   próxima subida. Para começar do zero, use `docker compose down -v`.

A senha padrão do usuário `sa` serve só para avaliação local: em qualquer outro uso, defina a
variável `SOPHIA_SENHA_DO_BANCO`. O SQL Server fica acessível apenas nesta máquina, na porta
`14333`, para quem quiser consultar o banco.

### Caminho B — Local, com .NET 8 e SQL Server Express

Pré-requisitos:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (`dotnet --version` deve
  mostrar 8.x);
- SQL Server Express com a instância `.\SQLEXPRESS` (qualquer edição 2019 ou mais nova);
- `sqlcmd` ou o SQL Server Management Studio (SSMS), para rodar o script.

1. Crie o banco com o script entregue:

   ```powershell
   sqlcmd -S .\SQLEXPRESS -E -C -i database/create-database.sql
   ```

   No SSMS, abra `database/create-database.sql` e execute (F5). O script pode ser rodado de
   novo sem apagar nada: ele só cria o que ainda não existe.

2. Confira se a conexão aponta para a sua instância. Ela fica em
   `src/Sophia.Web/appsettings.json`:

   ```json
   "Sophia": "Server=.\SQLEXPRESS;Database=Sophia;Integrated Security=true;TrustServerCertificate=true"
   ```

   Para outra instância, troque ali ou defina a variável de ambiente
   `ConnectionStrings__Sophia`.

3. Rode a aplicação:

   ```bash
   dotnet run --project src/Sophia.Web
   ```

   Ou abra `Sophia.sln` no Visual Studio, marque `Sophia.Web` como projeto de inicialização e
   pressione F5.

4. Abra http://localhost:5080. Para parar, `Ctrl+C` no terminal.

### Primeira venda, para conferir que está tudo certo

O passo a passo completo, com as telas, está no [Guia do usuário](docs/guia-do-usuario.md).

1. **Clientes → Novo cliente**: informe nome e CPF (por exemplo `529.982.247-25`) e salve.
   Tente cadastrar o mesmo CPF de novo: o sistema recusa com a mensagem no campo.
2. **Produtos → Novo produto**: cadastre dois produtos com valor unitário.
3. **Nova venda** (ou `F2` na tela de venda): busque o cliente pelo nome ou pelo CPF.
4. Lance os produtos (`F4`), ajuste as quantidades e veja o subtotal de cada item e o total
   da venda no visor.
5. Finalize (`Ctrl+Enter`). A venda aparece no **Painel** e em **Vendas**, com o detalhe dos
   itens.

### Se algo não subir

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| `A network-related or instance-specific error` | instância diferente de `.\SQLEXPRESS` ou serviço parado | inicie o serviço `SQL Server (SQLEXPRESS)` ou ajuste `ConnectionStrings__Sophia` |
| `Cannot open database "Sophia"` | o script não foi executado | rode o passo 1 do Caminho B |
| `address already in use` na porta 5080 ou 8080 | outra aplicação usa a porta | `dotnet run --project src/Sophia.Web --urls http://localhost:5090` ou troque a porta no `docker-compose.yml` |
| `web` não sobe no Docker | o `criar-banco` terminou com erro (por exemplo, senha fraca em `SOPHIA_SENHA_DO_BANCO`) | `docker compose logs criar-banco`; a senha precisa de 8+ caracteres com maiúscula, minúscula, número e símbolo |

---

## Testes

```bash
dotnet test
```

| Projeto | Tipo | O que cobre | Precisa de |
|---|---|---|---|
| `Sophia.Domain.Tests` | unidade | CPF, cliente, produto, venda, totais | nada |
| `Sophia.Application.Tests` | unidade | serviços: duplicidade, exclusão bloqueada, conflito de concorrência | nada |
| `Sophia.Specs` | BDD (Gherkin) | os cenários da especificação, em português | nada |
| `Sophia.Integration.Tests` | integração | script do banco, mapeamento do EF, índices únicos e chaves estrangeiras | SQL Server |
| `Sophia.E2E` | tela | cadastros e a venda completa no navegador | SQL Server e Chromium |

Os testes de integração e de tela criam um banco temporário a partir de
`database/create-database.sql` e o apagam ao final. O servidor usado é `.\SQLEXPRESS`; para
outro, defina `SOPHIA_TESTES_SQLSERVER` com o texto de conexão (sem o nome do banco).

Os testes de tela precisam do navegador do Playwright, instalado uma vez:

```bash
pwsh tests/Sophia.E2E/bin/Debug/net8.0/playwright.ps1 install chromium
```

Para usar o Google Chrome já instalado, defina `SOPHIA_E2E_CANAL=chrome`. Para **ver** o
navegador trabalhando, defina `SOPHIA_E2E_VISIVEL=1`: a janela abre e cada passo é
desacelerado.

Cada funcionalidade seguiu o ciclo do TDD: o cenário ou o teste foi escrito antes do código,
falhou, e o código veio em seguida até ficar verde. O histórico de commits mostra essa ordem.

---

## Arquitetura

Clean Architecture em quatro projetos. As dependências apontam só para dentro:

```
Sophia.Web ─────────────► Sophia.Application ──► Sophia.Domain
     │                           ▲
     └──► Sophia.Infrastructure ─┘
```

| Projeto | Responsabilidade | Depende de |
|---|---|---|
| `Sophia.Domain` | entidades (`Cliente`, `Produto`, `Venda`, `ItemVenda`), o valor `Cpf` e todas as regras de negócio | nada |
| `Sophia.Application` | casos de uso (`ClienteServico`, `ProdutoServico`, `VendaServico`), contratos de repositório, DTOs | Domain |
| `Sophia.Infrastructure` | EF Core, repositórios, unidade de trabalho, relógio no fuso de Brasília | Application |
| `Sophia.Web` | controllers, modelos de formulário, telas Razor, JavaScript | Application; Infrastructure só para registrar as dependências |

Caminho de uma venda:

1. a tela monta o formulário (`NovaVendaFormulario`) e o envia ao `VendasController`;
2. o controller converte em `DadosVenda` e chama `IVendaServico.RegistrarAsync`;
3. o serviço pede ao domínio `Venda.Registrar`, que valida e calcula; depois confere se o
   cliente e os produtos existem e grava pela unidade de trabalho;
4. o resultado volta como `Resultado`: sucesso com o número da venda, ou erros por campo, que o
   controller põe no `ModelState` para aparecerem ao lado do campo certo.

---

## Decisões

- **O domínio não lança exceção para regra de negócio.** `Cliente.Criar`, `Produto.Criar` e
  `Venda.Registrar` devolvem um `Resultado` com todos os erros de uma vez, cada um com o nome do
  campo. A tela mostra tudo o que está errado numa única tentativa, e exceção fica reservada
  para o que é de fato inesperado.
- **A unicidade é garantida duas vezes.** O serviço consulta antes de gravar, para dar a
  mensagem certa; o índice único do banco garante a regra mesmo quando dois operadores gravam
  ao mesmo tempo. A violação do índice é traduzida para a mesma mensagem amigável.
- **O valor unitário é copiado para o item da venda.** O enunciado pede informar o valor em
  cada lançamento; além disso, mudar o preço do produto depois não pode alterar vendas antigas.
- **Totais não são gravados.** Subtotal e total são calculados a partir dos itens, no domínio e
  na consulta. Gravar o resultado seria guardar o mesmo dado duas vezes.
- **O script SQL é a única fonte do esquema.** Não há migrations: o enunciado pede o script, e
  duas definições do banco abririam espaço para divergência. O mapeamento do EF espelha o
  script, e os testes de integração criam o banco pelo script e usam o EF em cima dele; se os
  dois divergirem, o teste quebra.
- **Leitura por projeção.** Listagens e comprovante de venda são consultas que já devolvem os
  DTOs, sem carregar as entidades inteiras.
- **CPF e telefone guardados só com os dígitos**, exibidos com máscara. A busca de cliente
  entende o termo: com letras, procura no nome; só com números, pelo começo do CPF.
- **A data de hoje vem de um relógio injetável no fuso de Brasília**, e não do relógio do
  servidor. No contêiner o relógio é UTC, e uma venda às 22h sugeriria a data do dia seguinte.
- **Front-end sem framework.** A tela de venda usa JavaScript puro em módulos, com
  autocompletar acessível por teclado e leitor de tela. Os totais na tela são conveniência: o
  servidor recalcula tudo.
- **O mesmo produto em duas linhas é recusado.** Na tela, escolher um produto já lançado soma 1
  à quantidade da linha existente.
- **Fora do escopo, de propósito:** autenticação, paginação das listas e cancelamento de venda.
  A estrutura em camadas permite acrescentá-los sem mexer no que existe.

---

## Estrutura do repositório

```
database/create-database.sql      script completo do banco
docs/guia-do-usuario.md           guia de operação, com as telas em docs/imagens
src/Sophia.Domain                 regras de negócio
src/Sophia.Application            casos de uso
src/Sophia.Infrastructure         EF Core e SQL Server
src/Sophia.Web                    ASP.NET Core MVC
tests/Sophia.Domain.Tests         unidade
tests/Sophia.Application.Tests    unidade
tests/Sophia.Specs                BDD (Gherkin)
tests/Sophia.Integration.Tests    integração com SQL Server
tests/Sophia.E2E                  tela com Playwright
tests/Sophia.Tests.Fakes          banco em memória dos testes de unidade e BDD
Dockerfile, docker-compose.yml    execução em contêineres
```
