# Sophia — Controle de Venda

Aplicação web para o controle de vendas a clientes, em **ASP.NET Core MVC (.NET 10)**, **C#** e
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

Operações: listar com busca por CPF ou nome (a lista se atualiza enquanto se digita), ordenar
pelas colunas, navegar em páginas de 25, cadastrar, alterar e excluir (com confirmação num
diálogo sobre a própria lista).

#### RF02 — Cadastro de produtos

| Campo | Obrigatório | Regra |
|---|---|---|
| Nome | sim | até 150 caracteres; único, sem diferenciar maiúsculas de minúsculas |
| Valor unitário | sim | maior que zero, no máximo duas casas decimais |

Operações: listar com busca por nome, ordenar por nome ou valor, navegar em páginas de 25,
cadastrar, alterar e excluir (com confirmação num diálogo).

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

Operações: listar as vendas (em páginas, com filtro de período e atalhos Hoje, últimos dias e
Mês, ordenadas por data ou total), registrar uma nova e consultar o comprovante de uma venda.

A tela é pensada para o balcão e funciona inteira pelo teclado: **F2** busca o cliente, **F4**
lança um produto, **Enter** na quantidade abre a próxima linha e **Ctrl+Enter** finaliza. No
celular, o total e o botão de finalizar ficam numa barra fixa, e a quantidade tem botões − e +.
Um produto removido por engano pode ser devolvido com **Desfazer**; sair da tela com produtos
lançados pede confirmação; cliente não encontrado oferece o cadastro numa nova aba. **Esc**
fecha as sugestões e, se a busca foi aberta pelo F2, devolve o cliente que estava escolhido.
Escolher de novo um produto já lançado soma 1 na linha dele, e a tela avisa em qual linha. Os
avisos de erro somem assim que o problema é resolvido, sem esperar um novo envio.

#### RF04 — Painel

A página inicial resume o movimento: quantidade e total das vendas de hoje, quantidade, total e
ticket médio do mês corrente, um gráfico com o total de cada um dos últimos 7 dias, os 5 produtos
mais vendidos do mês e as cinco vendas mais recentes. As quantidades (dias do gráfico, produtos,
vendas e o atalho de dias da lista de vendas) são parâmetros da seção `Sophia` do
`appsettings.json`.

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
| RN10 | Nome do produto não se repete; maiúsculas e espaços a mais não fazem outro nome | Já existe um produto com este nome. | Produtos › Não aceitar dois produtos com o mesmo nome |
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
| RN25 | Venda enviada duas vezes é registrada uma vez só: o segundo envio com o mesmo conteúdo devolve a venda já gravada; o mesmo formulário com outro conteúdo é recusado com aviso, para nenhuma venda se perder | — | Vendas › Enviar a mesma venda duas vezes registra uma só; Reaproveitar o formulário de uma venda registrada para outra venda é recusado |
| RN26 | O painel mostra o total de cada um dos últimos 7 dias, até hoje, inclusive os dias sem venda | — | Painel › Total de cada um dos últimos 7 dias, inclusive os dias sem venda |
| RN27 | O painel mostra os 5 produtos mais vendidos do mês por quantidade; no empate, vem o de maior total e depois a ordem alfabética | — | Painel › Os 5 produtos mais vendidos do mês, por quantidade |
| RN28 | A lista de vendas mostra 25 vendas por página, das mais recentes para as mais antigas; página pedida antes da primeira mostra a primeira, e depois da última mostra a última | — | Consulta de vendas › A lista de vendas mostra 25 por página, das mais recentes para as mais antigas |
| RN29 | As listas de clientes e de produtos mostram 25 por página, com a busca aplicada antes da divisão em páginas (clientes por CPF ou nome, produtos por nome) e ordenação por coluna: clientes por nome ou CPF, produtos por nome ou valor, em ordem crescente ou decrescente; o padrão é o nome em ordem alfabética, ordem desconhecida volta ao padrão e, no empate, vale a ordem de cadastro | — | Consulta de clientes › Ordenar a lista pela coluna escolhida; Consulta de produtos › Ordenar a lista pela coluna escolhida |
| RN30 | A lista de vendas pode ser filtrada por período, com as duas datas inclusive (data inicial depois da final é lida ao contrário; uma data só deixa o outro lado aberto), e ordenada pela data ou pelo total, em ordem crescente ou decrescente; o padrão é da mais recente para a mais antiga e, no empate, vale o número da venda no mesmo sentido | — | Consulta de vendas › Filtrar as vendas por período, com as duas datas inclusive; Consulta de vendas › Ordenar pelo total da venda |

### 6. Requisitos não funcionais

| Código | Requisito |
|---|---|
| RNF01 | ASP.NET Core MVC (.NET 10, versão de suporte longo) com C#. |
| RNF02 | SQL Server (Express), acesso via Entity Framework Core. |
| RNF03 | Banco com chaves primárias e estrangeiras, índices, campos obrigatórios e normalização; o script de criação é entregue em `database/create-database.sql`. |
| RNF04 | Interface simples e moderna, em português do Brasil (datas `dd/mm/aaaa`, valores `R$ 1.234,56`), com tema claro, escuro ou automático. Funciona a partir de 360 px de largura sem rolagem lateral, com alvos de toque de 44 px e contraste WCAG AA. |
| RNF05 | Validação em dois níveis: no navegador, para resposta imediata, e no servidor, que é a fonte da verdade. |
| RNF06 | Erros inesperados mostram uma página amigável, sem detalhes técnicos. |
| RNF07 | Executável com Docker (`docker compose up`), sem instalar .NET ou SQL Server na máquina. |
| RNF08 | Sem valores fixos no código: textos da interface e mensagens em arquivos de recursos (`.resx`), parâmetros de negócio (tamanho da página, dias do gráfico etc.) na seção `Sophia` do `appsettings.json`, endereços do JavaScript gerados pelas rotas e cores só em variáveis de tema. |
| RNF09 | Qualidade verificada no build: análise estática (analisadores do .NET e SonarAnalyzer), complexidade ciclomática até 10 por método, índice de manutenibilidade de no mínimo 60 e formatação conferida; ESLint no JavaScript. Qualquer violação quebra o build. |

### 7. Stack

Cada peça entrou por uma razão concreta, e o critério geral foi o mesmo: usar o que o próprio
ecossistema .NET oferece, evitar etapa de build no front-end e não depender de serviço externo
para compilar, testar ou rodar.

| Camada | Tecnologia | Por que usamos |
|---|---|---|
| Linguagem e plataforma | C# 14 · .NET 10 | O enunciado pede ASP.NET Core MVC, e o .NET 10 é a versão LTS (suporte de longo prazo) atual, com correções garantidas até novembro de 2028. O projeto nasceu no .NET 8 e foi atualizado quando o suporte dele terminou, em 10/11/2026. O .NET 11, mais novo, tem suporte curto (STS) e não compensa para um sistema de loja. |
| Web | ASP.NET Core MVC · Razor | Pedido no enunciado. As páginas chegam prontas do servidor, então a tela funciona antes de qualquer JavaScript, e o framework já entrega validação do modelo, proteção contra envio forjado (antiforgery), localização e página de erro. |
| Interface | Bootstrap 5.3 · CSS próprio por área · JavaScript puro em módulos ES | O Bootstrap dá grade, formulários e o tema escuro (`data-bs-theme`) prontos e já cuidados em acessibilidade. O CSS próprio, dividido por área em `wwwroot/css`, aplica a identidade da Sophia. O JavaScript roda direto no navegador, sem npm nem bundler na aplicação: o que precisa de interação (caixa, busca enquanto digita, diálogo) é pequeno demais para justificar um framework. |
| Validação no navegador | jQuery Validation · validação discreta do ASP.NET Core | É o par que transforma as regras (`DataAnnotations`) do modelo em validação no navegador, com as mesmas mensagens do servidor. O jQuery entra só por causa dele, e só nas telas com formulário de cadastro. |
| Tipografia e ícones | Poppins (arquivos próprios em `wwwroot/fontes`, licença OFL) · ícones Lucide em sprite SVG | A Poppins é a fonte da identidade da Sophia e tem algarismos tabulares, que alinham os valores em coluna. Os arquivos ficam no próprio Sophia: a tela não depende da internet nem consulta o Google a cada página. Os ícones Lucide (licença ISC) ficam num sprite SVG da própria página: uma requisição a menos e cor herdada do texto, inclusive no tema escuro. |
| Acesso a dados | Entity Framework Core 10 | Consultas tipadas e sempre parametrizadas (nada de SQL montado com texto do usuário), projeção direta para os DTOs da tela e tradução da violação de índice único para a mensagem do negócio. Sem migrations: o enunciado pede o script, e ele é a única fonte do esquema. |
| Banco de dados | SQL Server Express (2019 ou mais novo local, 2022 no Docker) | Edição gratuita do SQL Server. O mesmo script T-SQL roda no local e no contêiner, e recursos como o índice único filtrado garantem regras do negócio, como a RN25, no próprio banco. |
| Testes de unidade e de serviço | xUnit · FluentAssertions 6 | O xUnit é o padrão do ecossistema .NET, com fixtures que compartilham um banco por coleção de testes. O FluentAssertions faz a falha dizer o esperado e o encontrado em forma de frase. A versão 6 tem licença Apache 2.0; a 8 passou a ser paga para uso comercial. |
| Especificação executável (BDD) | Reqnroll · Gherkin em português | Sucessor de código aberto do SpecFlow. Os cenários desta especificação rodam como testes, então o documento não fica desatualizado em relação ao código. |
| Testes de integração | xUnit · SQL Server real | Conferem o que um dublê não conferiria: o script do banco, os índices, o SQL que o EF gera e a corrida entre duas gravações. |
| Testes de tela (E2E) | Playwright para .NET | Opera um Chromium de verdade, na mesma linguagem dos outros testes. Espera os elementos sozinho, o que evita testes instáveis, e emula celular, toque, rede lenta e tema escuro. |
| Contêineres | Docker · Docker Compose | Quem avalia sobe o banco, cria as tabelas, carrega os dados de demonstração e abre a aplicação com um comando, sem instalar .NET nem SQL Server. |
| Textos | `IStringLocalizer` · arquivos `.resx` | Mecanismo nativo do ASP.NET Core. Todas as frases da tela ficam num lugar só, e um teste confere que nenhuma chave falta e nenhuma sobra. |
| Análise estática | analisadores do .NET · SonarAnalyzer.CSharp · métricas de código · ESLint · `dotnet format` | Defeito comum, complexidade alta e formatação fora do padrão quebram o build, na máquina e no CI. As regras do SonarQube rodam como analisador, sem depender de servidor. |
| Integração contínua | GitHub Actions | O repositório está no GitHub. A cada PR roda formatação, ESLint, build e a suíte completa, com SQL Server em contêiner, e o merge só acontece com tudo verde. |

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
| RN25 | `RegistroDeVendas.feature` › Enviar a mesma venda duas vezes registra uma só; Reaproveitar o formulário de uma venda registrada para outra venda é recusado | `Sophia.Domain.Tests` (Venda), `Sophia.Application.Tests` (VendaServico, inclusive a corrida entre dois envios) | `Sophia.Integration.Tests` (índice único filtrado, corrida no SQL Server, atualização do banco pelo script) |
| RN26 | `PainelDeVendas.feature` › Total de cada um dos últimos 7 dias | `Sophia.Application.Tests` (PainelServico) | `Sophia.Integration.Tests` (totais por dia agregados no SQL Server) |
| RN27 | `PainelDeVendas.feature` › Os 5 produtos mais vendidos do mês | `Sophia.Application.Tests` (PainelServico) | `Sophia.Integration.Tests` (mais vendidos agregados no SQL Server) |
| RN28 | `ConsultaDeVendas.feature` | `Sophia.Application.Tests` (Pagina, VendaServico: tamanho, ordem e limites de página) | `Sophia.Integration.Tests` (contagem e recorte com `OFFSET`/`FETCH` no SQL Server) |
| RN29 | `ConsultaDeClientes.feature`, `ConsultaDeProdutos.feature` | `Sophia.Application.Tests` (Ordenacao, ClienteServico, ProdutoServico: busca, ordem, desempate e limites de página) | `Sophia.Integration.Tests` (contagem, `ORDER BY` e recorte com `OFFSET`/`FETCH` no SQL Server) |
| RN30 | `ConsultaDeVendas.feature` | `Sophia.Application.Tests` (ConsultaDeVendas, VendaServico: período, ordem pela data ou pelo total) | `Sophia.Integration.Tests` (período e total somado no SQL Server, ligação das datas `yyyy-MM-dd` da consulta) |

---

## Como executar

Roteiro do zero até a primeira venda. Escolha **um** dos dois caminhos: Docker (nada para
instalar além do Docker) ou local (.NET 10 e SQL Server Express na máquina).

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
   2. `criar-banco`: executa `database/create-database.sql`, aplica os
      [dados de demonstração](#dados-de-demonstração) se o banco estiver vazio e termina;
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

Para subir com o banco vazio, sem os dados de demonstração, defina
`SOPHIA_DADOS_DE_DEMONSTRACAO=nao` (no ambiente ou no arquivo `.env`) antes do primeiro `up`.

### Caminho B — Local, com .NET 10 e SQL Server Express

Pré-requisitos:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`dotnet --version` deve
  mostrar 10.x);
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
   "Sophia": "Server=.\\SQLEXPRESS;Database=Sophia;Integrated Security=true;TrustServerCertificate=true"
   ```

   Para outra instância, troque ali ou defina a variável de ambiente
   `ConnectionStrings__Sophia`.

3. Rode a aplicação:

   ```bash
   dotnet run --project src/Sophia.Web
   ```

   Ou abra `Sophia.sln` no Visual Studio 2026 ou mais novo, marque `Sophia.Web` como projeto de inicialização e
   pressione F5.

4. Abra http://localhost:5080. Para parar, `Ctrl+C` no terminal.

### Dados de demonstração

O script `database/carga-de-demonstracao.sql` enche um banco **vazio** com uma loja de
informática: 97 produtos (catálogo de um sistema de pedidos), 320 clientes com CPF válido e cerca
de 1.450 vendas nos últimos 120 dias, com datas calculadas a partir do dia em que a carga roda, para
o painel e os gráficos terem movimento. Se o banco já tiver dados, o script avisa e não grava nada.

No Docker a carga roda por padrão, na subida. No caminho local, rode-a depois de criar o banco:

```powershell
sqlcmd -S .\SQLEXPRESS -E -C -d Sophia -i database/carga-de-demonstracao.sql
```

No Docker, para carregar num banco que subiu vazio (com `SOPHIA_DADOS_DE_DEMONSTRACAO=nao`), troque
a variável para `sim` e rode `docker compose up -d criar-banco`.

### Primeira venda, para conferir que está tudo certo

O passo a passo completo, com as telas, está no [Guia do usuário](docs/guia-do-usuario.md).

1. **Cliente → Novo cliente**: informe nome e CPF (por exemplo `529.982.247-25`) e salve.
   Tente cadastrar o mesmo CPF de novo: o sistema recusa com a mensagem no campo.
2. **Produto → Novo produto**: cadastre dois produtos com valor unitário.
3. **Nova venda** (ou `F2` na tela de venda): busque o cliente pelo nome ou pelo CPF.
4. Lance os produtos (`F4`), ajuste as quantidades e veja o subtotal de cada item e o total
   da venda no visor.
5. Finalize (`Ctrl+Enter`). A venda aparece no **Painel** e em **Venda**, com o comprovante.

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

| Projeto | Tipo | Testes | O que cobre | Precisa de |
|---|---|---|---|---|
| `Sophia.Domain.Tests` | unidade | 102 | CPF, cliente, produto, venda, totais, mensagens do domínio | nada |
| `Sophia.Application.Tests` | aplicação | 171 | serviços sobre o banco real: duplicidade, exclusão bloqueada, corrida entre duas gravações, páginas, ordem e período, parâmetros | SQL Server |
| `Sophia.Specs` | BDD (Gherkin) | 81 | os cenários da especificação, em português, sobre o banco real | SQL Server |
| `Sophia.Integration.Tests` | integração | 107 | script do banco, carga de demonstração, mapeamento do EF conferido contra o script, consultas e busca sem acento, leitura de números e datas do formulário, subida com parâmetros inválidos, chaves dos textos da interface | SQL Server |
| `Sophia.E2E` | tela | 228 | cadastros, listas, caixa, celular, prevenção de erros, retorno ao operador, tema e gráficos, máscaras, cabeçalhos de segurança e política de conteúdo, no navegador | SQL Server e Chromium |

Os testes da aplicação, os cenários BDD, os de integração e os de tela criam um banco
temporário a partir de `database/create-database.sql` e o apagam ao final; nenhum deles usa
dublê de repositório. O apoio comum fica em `tests/Sophia.Testes.Banco`. O servidor usado é `.\SQLEXPRESS`; para
outro, defina `SOPHIA_TESTES_SQLSERVER` com o texto de conexão (sem o nome do banco).

Os testes de tela precisam do navegador do Playwright, instalado uma vez:

```bash
pwsh tests/Sophia.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
```

Para usar o Google Chrome já instalado, defina `SOPHIA_E2E_CANAL=chrome`. Para **ver** o
navegador trabalhando, defina `SOPHIA_E2E_VISIVEL=1`: a janela abre e cada passo é
desacelerado.

Formatação e JavaScript, como no CI:

```bash
dotnet format --verify-no-changes
npm install --no-save eslint@9.17.0 @eslint/js@9.17.0 globals@15.14.0
npx eslint --config eslint.config.mjs src/Sophia.Web/wwwroot/js
```

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
| `Sophia.Application` | casos de uso (`ClienteServico`, `ProdutoServico`, `VendaServico`, `PainelServico`), contratos de repositório (gravação) e de consulta (leitura), DTOs | Domain |
| `Sophia.Infrastructure` | EF Core, repositórios que gravam (`VendaRepositorio`), consultas que só leem (`ConsultasDeVendas`, `ConsultasDoPainel`), unidade de trabalho, relógio no fuso de Brasília | Application |
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
  script, e os testes de integração criam o banco pelo script e usam o EF em cima dele. O teste
  `MapeamentoContraOScriptTests` compara os índices e os CHECKs dos dois lados: se alguém
  acrescentar um índice só no script ou só no EF, ele quebra.
- **Leitura por projeção.** Listagens e comprovante de venda são consultas que já devolvem os
  DTOs, sem carregar as entidades inteiras.
- **CPF e telefone guardados só com os dígitos**, exibidos com máscara. A busca de cliente
  entende o termo: com letras, procura no nome; só com números, pelo começo do CPF; só com
  pontuação, procura esse trecho no nome ("-" acha os nomes com hífen) em vez de listar todos.
  Espaços repetidos no termo contam como um só. A busca por nome, de cliente e de produto,
  ignora acentos e maiúsculas ("joao" acha "João") sem mudar o agrupamento do banco: a consulta
  compara com `Latin1_General_CI_AI`. Um CPF gravado direto no banco fora do dígito verificador é
  lido como está, para as telas não caírem; ao alterar esse cliente, o cadastro exige um CPF
  válido.
- **A data de hoje vem de um relógio injetável no fuso de Brasília**, e não do relógio do
  servidor. No contêiner o relógio é UTC, e uma venda às 22h sugeriria a data do dia seguinte.
- **Front-end sem framework.** A tela de venda usa JavaScript puro em módulos, com
  autocompletar acessível por teclado e leitor de tela. Os totais na tela são conveniência: o
  servidor recalcula tudo.
- **O mesmo produto em duas linhas é recusado.** Na tela, escolher um produto já lançado soma 1
  à quantidade da linha existente, avisa em qual linha e a realça.
- **Números no formato brasileiro, com a mesma regra no navegador e no servidor.** Vírgula é
  decimal e ponto é milhar (`1.250,00`). Sem vírgula, um ponto seguido de uma ou duas casas é
  decimal (`12.50` vale 12,50) e grupos de três depois do ponto são milhar (`1.250`); qualquer
  outra forma é recusada com aviso, em vez de virar outro valor. A regra mora em `formato.js` e
  em `Apoio/DecimalPtBr.cs`, e um teste de integração confere os mesmos casos no servidor.
- **O domínio fala de campo e de linha, não de tela.** Um erro diz o campo (`ProdutoId`) e,
  num item da venda, a linha; só a Web monta a chave do formulário (`Itens[2].ProdutoId`), em
  `Apoio/ModelStateExtensions.cs`. Mudar o nome de um campo na tela não mexe no domínio.
- **O comprovante mostra o nome atual do produto.** O item da venda guarda o produto e o valor
  unitário da época; o nome vem do cadastro. O enunciado pede o preço congelado, não o nome, e
  renomear um produto costuma ser correção de digitação. Se o nome histórico passar a importar,
  basta uma coluna no item.
- **Venda registrada uma vez só, mesmo com duplo envio.** A tela trava o botão, mas a defesa
  real está no servidor: cada formulário de venda leva uma chave única, gravada com índice único
  filtrado. O segundo envio devolve a venda já gravada, e a corrida entre dois envios simultâneos
  cai no índice e é resolvida da mesma forma.
- **Testes sem dublês.** Os serviços e os cenários BDD rodam no SQL Server real, sobre um banco
  temporário criado pelo script entregue. Assim o teste cobre também o SQL que o EF gera, e a
  corrida entre duas gravações é provocada de verdade, com dois contextos. O custo é precisar de
  SQL Server para quase toda a suíte; só o domínio roda sem banco.
- **Nada fixo no código.** Textos em `Recursos/Textos.resx` (Web) e `Mensagens.resx` (domínio e
  aplicação); um teste confere que toda chave usada existe e que nenhuma sobra. A aplicação
  tem um idioma só: o `.resx` não está ali para traduzir, e sim para deixar todo o texto que o
  operador lê num lugar só, revisável sem abrir código. Parâmetros de
  negócio na seção `Sophia` do `appsettings.json`, validados na subida: valor fora da faixa impede
  a aplicação de subir. O JavaScript recebe endereços e textos por atributos `data-*` gerados pela
  view. Exceção consciente: a mensagem de configuração ausente no `Program`, que é para quem
  instala, não para quem usa a tela.
- **DRY sem herança forçada.** Listas, busca, cabeçalho ordenável, paginador e páginas de
  exclusão são partials comuns; listar, gravar e excluir cadastro ficam em `Apoio/AcoesDeCadastro`
  e o salvamento com tradução de duplicidade em `GravacaoDeCadastro`. Os controllers e os serviços
  de cliente e de produto continuam separados e parecidos: com dois casos, uma classe base
  genérica custaria mais leitura do que economizaria.
- **Duas regras de qualidade valem só para o código de produção.** Nos testes, nomes com
  sublinhado (CA1707) deixam o nome do teste legível como frase, e o índice de manutenibilidade
  (CA1505) não se aplica: um teste é um roteiro declarativo de preparar, agir e conferir.
- **Uma regra do Sonar fica desligada no código de produção: S6967** (conferir o `ModelState` em
  cada action). A conferência existe, mas num lugar só: o filtro `RecusarConsultaInvalida` recusa
  toda consulta GET com valor inválido, e o envio de formulário é conferido em `AcoesDeCadastro`.
  Repetir a mesma linha em cada action era o que a regra pedia.
- **Docker para avaliação.** O `docker-compose.yml` conecta como `sa`, com senha padrão se
  `SOPHIA_SENHA_DO_BANCO` não for definida, e o script cria o banco com o nome fixo `Sophia`:
  se ele já existir, o script reaproveita e só cria o que falta, sem apagar dados. Fora da
  avaliação, defina a senha e use um login próprio da aplicação.
- **Identidade visual da Sophia.** Paleta, fonte e logo seguem o site da marca; o azul-petróleo
  foi escurecido de `#007B9E` para `#007596` porque o original não atingia contraste AA sobre o
  fundo claro.
- **Segurança no que o sistema faz.** Todo POST exige o token antiforgery; as consultas passam
  pelo EF com parâmetros; o Razor codifica toda saída e o JavaScript só escreve na página por
  `textContent`. Toda resposta leva `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy` e uma política de conteúdo (CSP) que só aceita script, estilo, imagem e
  conexão do próprio endereço, inclusive a fonte: nenhum script ou estilo embutido no
  HTML, e por isso as barras do painel são SVG com a medida em atributo. As somas de unidades
  usam inteiro de 64 bits, para uma quantidade digitada com zeros a mais não derrubar o painel.
- **Fora do escopo, de propósito:** autenticação, HTTPS e cancelamento de venda. A estrutura em camadas
  permite acrescentá-los sem mexer no que existe.

---

## Qualidade

O build falha se qualquer item abaixo for violado, na máquina e no CI:

- analisadores do .NET e SonarAnalyzer.CSharp, com aviso tratado como erro;
- complexidade ciclomática até 10 por método (CA1502) e índice de manutenibilidade de no mínimo
  60 (CA1505);
- estilo de código no build e `dotnet format --verify-no-changes`;
- ESLint no JavaScript: complexidade até 10, funções de até 60 linhas e no máximo 3 níveis de
  aninhamento.

---

## Marca

O nome Sophia, o logotipo e as cores da identidade visual pertencem à Sophia
([sophia.com.br](https://sophia.com.br/)) e foram usados apenas para este teste técnico.

---

## Estrutura do repositório

```
database/create-database.sql      script completo do banco
database/carga-de-demonstracao.sql dados de demonstração para um banco vazio
docs/guia-do-usuario.md           guia de operação, com as telas em docs/imagens
src/Sophia.Domain                 regras de negócio
src/Sophia.Application            casos de uso
src/Sophia.Infrastructure         EF Core e SQL Server
src/Sophia.Web                    ASP.NET Core MVC
tests/Sophia.Domain.Tests         unidade
tests/Sophia.Application.Tests    serviços sobre o SQL Server
tests/Sophia.Specs                BDD (Gherkin)
tests/Sophia.Integration.Tests    integração com SQL Server
tests/Sophia.E2E                  tela com Playwright
tests/Sophia.Testes.Banco         banco temporário e composição dos serviços para os testes
src/Sophia.Web/Recursos           textos da interface (Textos.resx)
src/Sophia.Web/wwwroot/css        estilo por área: base, layout, componentes, responsivo, painel, pessoas, cupom, listas e caixa
src/Sophia.Web/wwwroot/js         módulos ES: comum/ (todas as telas), caixa/ (nova venda), lista, confirmar e autocompletar
Dockerfile, docker-compose.yml    execução em contêineres
eslint.config.mjs                 regras do JavaScript
CodeMetricsConfig.txt             limites de complexidade e manutenibilidade
```
