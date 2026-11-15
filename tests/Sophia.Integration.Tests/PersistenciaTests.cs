using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sophia.Application;
using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Painel;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Infrastructure.Persistencia;
using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class PersistenciaTests(BancoDeTestes banco) : IAsyncLifetime
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);
    private static readonly Ordenacao<ColunaDeVendas> MaisRecentes = new(ColunaDeVendas.Data, Decrescente: true);

    public Task InitializeAsync() => banco.LimparAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Grava_e_le_o_cliente_com_o_cpf_convertido()
    {
        var id = await CadastrarClienteAsync("529.982.247-25", "Maria Santos", "(11) 98765-4321");

        await using var contexto = banco.NovoContexto();
        var cliente = await new ClienteRepositorio(contexto).ObterAsync(id);

        cliente!.Cpf.Formatado.Should().Be("529.982.247-25");
        cliente.Telefone.Should().Be("11987654321");
    }

    [Fact]
    public async Task Busca_clientes_pelo_inicio_do_cpf_ou_por_trecho_do_nome()
    {
        await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        await CadastrarClienteAsync("111.444.777-35", "João Lima");

        await using var contexto = banco.NovoContexto();
        var repositorio = new ClienteRepositorio(contexto);

        (await repositorio.ListarAsync(BuscaDeCliente.Interpretar("529982"), null)).Select(c => c.Nome)
            .Should().Equal("Maria Santos");
        (await repositorio.ListarAsync(BuscaDeCliente.Interpretar("lim"), null)).Select(c => c.Nome)
            .Should().Equal("João Lima");
        (await repositorio.ListarAsync(BuscaDeCliente.Interpretar(null), 1)).Select(c => c.Nome)
            .Should().Equal("João Lima");
    }

    [Fact]
    public async Task A_busca_de_clientes_pelo_nome_junta_os_espacos_repetidos_do_termo()
    {
        await CadastrarClienteAsync("529.982.247-25", "Maria Silva");

        await using var contexto = banco.NovoContexto();
        var encontrados = await new ClienteRepositorio(contexto).ListarAsync(BuscaDeCliente.Interpretar("Maria  Silva"), null);

        encontrados.Select(c => c.Nome).Should().Equal("Maria Silva");
    }

    [Fact]
    public async Task A_busca_de_clientes_pelo_nome_nao_diferencia_acentos()
    {
        await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        await CadastrarClienteAsync("111.444.777-35", "João Lima");

        await using var contexto = banco.NovoContexto();
        var repositorio = new ClienteRepositorio(contexto);
        var busca = BuscaDeCliente.Interpretar("joao");

        (await repositorio.ListarAsync(busca, null)).Select(c => c.Nome).Should().Equal("João Lima");
        (await repositorio.ListarPaginaAsync(busca, new(ColunaDeClientes.Nome, Decrescente: false), 1, 25))
            .Itens.Select(c => c.Nome).Should().Equal("João Lima");
    }

    [Fact]
    public async Task A_busca_de_clientes_so_com_pontuacao_nao_traz_ninguem()
    {
        await CadastrarClienteAsync("529.982.247-25", "Maria Santos");

        await using var contexto = banco.NovoContexto();
        var repositorio = new ClienteRepositorio(contexto);
        var busca = BuscaDeCliente.Interpretar("-");

        (await repositorio.ListarAsync(busca, null)).Should().BeEmpty();
        (await repositorio.ListarPaginaAsync(busca, new(ColunaDeClientes.Nome, Decrescente: false), 1, 25))
            .TotalDeItens.Should().Be(0);
    }

    [Fact]
    public async Task A_busca_de_produtos_pelo_nome_nao_diferencia_acentos()
    {
        await CadastrarProdutoAsync("Açúcar cristal", 5m);
        await CadastrarProdutoAsync("Caneta azul", 2.50m);

        await using var contexto = banco.NovoContexto();
        var repositorio = new ProdutoRepositorio(contexto);

        (await repositorio.ListarAsync("acucar", null)).Select(p => p.Nome).Should().Equal("Açúcar cristal");
        (await repositorio.ListarPaginaAsync("ACUCAR", new(ColunaDeProdutos.Nome, Decrescente: false), 1, 25))
            .Itens.Select(p => p.Nome).Should().Equal("Açúcar cristal");
    }

    [Fact]
    public async Task O_indice_unico_de_cpf_vira_conflito_de_valor_duplicado()
    {
        await CadastrarClienteAsync("529.982.247-25", "Maria Santos");

        var gravarDeNovo = () => CadastrarClienteAsync("52998224725", "Outra Maria");

        (await gravarDeNovo.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.ValorDuplicado);
    }

    [Fact]
    public async Task O_nome_do_produto_e_unico_sem_diferenciar_maiusculas()
    {
        await CadastrarProdutoAsync("Caneta azul", 2.50m);

        await using var contexto = banco.NovoContexto();
        var existe = await new ProdutoRepositorio(contexto).ExisteComNomeAsync("CANETA AZUL", excetoId: null);

        existe.Should().BeTrue();
    }

    [Fact]
    public async Task O_nome_do_produto_e_unico_sem_diferenciar_acentos()
    {
        await CadastrarProdutoAsync("Café Torrado", 32m);

        await using var contexto = banco.NovoContexto();
        (await new ProdutoRepositorio(contexto).ExisteComNomeAsync("Cafe Torrado", excetoId: null)).Should().BeTrue();
        var gravarDeNovo = () => CadastrarProdutoAsync("CAFE TORRADO", 30m);

        (await gravarDeNovo.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.ValorDuplicado);
    }

    [Fact]
    public async Task Grava_a_venda_com_itens_e_le_resumo_e_detalhe()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);

        var vendaId = await RegistrarVendaAsync(maria, [new(caneta, 10, 2.50m), new(caderno, 2, 18.90m)]);

        await using var contexto = banco.NovoContexto();
        var consultas = new ConsultasDeVendas(contexto);
        (await consultas.ListarAsync()).Should().ContainSingle()
            .Which.Should().Be(new VendaResumoDto(vendaId, Hoje, "529.982.247-25", "Maria Santos", 2, 62.80m));
        var detalhe = await consultas.ObterDetalheAsync(vendaId);
        detalhe!.Itens.Should().Equal(
            new ItemVendaDto("Caneta azul", 10, 2.50m, 25.00m),
            new ItemVendaDto("Caderno 96 folhas", 2, 18.90m, 37.80m));
        detalhe.Total.Should().Be(62.80m);
    }

    [Fact]
    public async Task Resume_as_vendas_do_periodo_e_limita_as_ultimas()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);
        await RegistrarVendaAsync(maria, [new(caneta, 10, 2.50m), new(caderno, 1, 18.90m)]);
        await RegistrarVendaAsync(maria, [new(caneta, 2, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        var painel = new ConsultasDoPainel(contexto);

        (await painel.ResumirAsync(Hoje, Hoje)).Should().Be(new ResumoDoPeriodo(2, 48.90m));
        (await painel.ResumirAsync(Hoje.AddDays(1), Hoje.AddDays(30))).Should().Be(ResumoDoPeriodo.Vazio);
        (await new ConsultasDeVendas(contexto).ListarAsync(limite: 1)).Should().ContainSingle().Which.Total.Should().Be(5.00m);
    }

    [Fact]
    public async Task Sabe_se_o_cliente_e_o_produto_ja_tem_vendas()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var joao = await CadastrarClienteAsync("111.444.777-35", "João Lima");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var lapis = await CadastrarProdutoAsync("Lápis", 1m);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        var clientes = new ClienteRepositorio(contexto);
        var produtos = new ProdutoRepositorio(contexto);

        (await clientes.PossuiVendasAsync(maria)).Should().BeTrue();
        (await clientes.PossuiVendasAsync(joao)).Should().BeFalse();
        (await produtos.FoiVendidoAsync(caneta)).Should().BeTrue();
        (await produtos.FoiVendidoAsync(lapis)).Should().BeFalse();
        (await produtos.FiltrarExistentesAsync([caneta, lapis, 999])).Should().BeEquivalentTo([caneta, lapis]);
    }

    [Fact]
    public async Task A_chave_estrangeira_impede_excluir_cliente_com_venda()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        var repositorio = new ClienteRepositorio(contexto);
        repositorio.Remover((await repositorio.ObterAsync(maria))!);
        var salvar = () => new UnidadeDeTrabalho(contexto).SalvarAsync();

        (await salvar.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.RegistroEmUso);
    }

    [Fact]
    public async Task Venda_que_aponta_para_cadastro_inexistente_e_restricao_violada()
    {
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);

        var gravar = () => RegistrarVendaAsync(999, [new(caneta, 1, 2.50m)]);

        (await gravar.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.RestricaoViolada);
    }

    [Fact]
    public async Task Restricao_de_verificacao_violada_por_fora_do_dominio_e_restricao_violada_com_a_causa_do_banco()
    {
        await using var contexto = banco.NovoContexto();
        var produto = Produto.Criar(new DadosProduto("Caneta azul", 2.50m)).Valor;
        new ProdutoRepositorio(contexto).Adicionar(produto);
        contexto.Entry(produto).Property(p => p.ValorUnitario).CurrentValue = -1m;

        var salvar = () => new UnidadeDeTrabalho(contexto).SalvarAsync();

        var conflito = (await salvar.Should().ThrowAsync<ConflitoAoSalvarException>()).Which;
        conflito.Tipo.Should().Be(TipoDeConflito.RestricaoViolada);
        conflito.InnerException.Should().BeOfType<DbUpdateException>()
            .Which.InnerException.Should().BeOfType<SqlException>()
            .Which.Message.Should().Contain("CK_Produtos_ValorUnitario_Positivo");
    }

    [Fact]
    public async Task A_chave_estrangeira_impede_excluir_produto_vendido()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        var repositorio = new ProdutoRepositorio(contexto);
        repositorio.Remover((await repositorio.ObterAsync(caneta))!);
        var salvar = () => new UnidadeDeTrabalho(contexto).SalvarAsync();

        (await salvar.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.RegistroEmUso);
    }

    [Fact]
    public async Task O_indice_unico_da_chave_de_registro_barra_a_segunda_gravacao()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var chave = Guid.NewGuid();
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], chave);

        var gravarDeNovo = () => RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], chave);

        (await gravarDeNovo.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.ValorDuplicado);
    }

    [Fact]
    public async Task Vendas_sem_chave_de_registro_nao_esbarram_no_indice_unico()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);

        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        (await new ConsultasDeVendas(contexto).ListarAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Acha_o_numero_da_venda_pela_chave_de_registro()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var chave = Guid.NewGuid();
        var vendaId = await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], chave);

        await using var contexto = banco.NovoContexto();
        var repositorio = new VendaRepositorio(contexto);

        (await repositorio.ObterPelaChaveAsync(chave))!.Id.Should().Be(vendaId);
        (await repositorio.ObterPelaChaveAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task Totaliza_por_dia_no_banco_so_os_dias_do_periodo_que_tem_venda()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], data: new DateOnly(2026, 11, 6));
        await RegistrarVendaAsync(maria, [new(caneta, 2, 2.50m)], data: new DateOnly(2026, 11, 7));
        await RegistrarVendaAsync(maria, [new(caderno, 1, 18.90m)], data: new DateOnly(2026, 11, 7));
        await RegistrarVendaAsync(maria, [new(caneta, 10, 2.50m), new(caderno, 2, 18.90m)], data: Hoje);

        await using var contexto = banco.NovoContexto();
        var totais = await new ConsultasDoPainel(contexto).TotaisPorDiaAsync(new DateOnly(2026, 11, 7), Hoje);

        totais.Should().Equal(
            new TotalDoDia(new DateOnly(2026, 11, 7), 23.90m, 2),
            new TotalDoDia(Hoje, 62.80m, 1));
    }

    [Fact]
    public async Task Agrupa_os_mais_vendidos_no_banco_por_quantidade_total_e_nome()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);
        var lapis = await CadastrarProdutoAsync("Lápis", 1.50m);
        var borracha = await CadastrarProdutoAsync("Borracha", 1.50m);
        var regua = await CadastrarProdutoAsync("Régua", 4m);
        await RegistrarVendaAsync(maria, [new(regua, 90, 4m)], data: new DateOnly(2026, 10, 31));
        await RegistrarVendaAsync(maria, [new(caneta, 6, 2.50m), new(lapis, 3, 1.50m)], data: new DateOnly(2026, 11, 2));
        await RegistrarVendaAsync(maria, [new(caneta, 4, 2.40m), new(caderno, 3, 18.90m)], data: new DateOnly(2026, 11, 5));
        await RegistrarVendaAsync(maria, [new(borracha, 3, 1.50m), new(regua, 1, 4m)], data: Hoje);

        await using var contexto = banco.NovoContexto();
        var painel = new ConsultasDoPainel(contexto);
        var doMes = await painel.MaisVendidosAsync(new DateOnly(2026, 11, 1), Hoje, 4);

        doMes.Should().Equal(
            new ProdutoMaisVendido(caneta, "Caneta azul", 10, 24.60m),
            new ProdutoMaisVendido(caderno, "Caderno 96 folhas", 3, 56.70m),
            new ProdutoMaisVendido(borracha, "Borracha", 3, 4.50m),
            new ProdutoMaisVendido(lapis, "Lápis", 3, 4.50m));
        (await painel.MaisVendidosAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31), 5))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Soma_dos_mais_vendidos_passa_do_limite_de_um_inteiro_sem_falhar()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 0.01m);
        await banco.ExecutarAsync($"""
            INSERT INTO dbo.Vendas (Data, ClienteId)
            SELECT TOP (2148) IIF(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) % 2 = 0, '2026-11-02', '2026-11-13'), {maria}
            FROM sys.all_objects a CROSS JOIN sys.all_objects b;
            INSERT INTO dbo.VendaItens (VendaId, ProdutoId, Quantidade, ValorUnitario)
            SELECT Id, {caneta}, 999999, 0.01 FROM dbo.Vendas;
            """);

        await using var contexto = banco.NovoContexto();
        var doMes = await new ConsultasDoPainel(contexto).MaisVendidosAsync(new DateOnly(2026, 11, 1), Hoje, 5);

        doMes.Should().Equal(new ProdutoMaisVendido(caneta, "Caneta azul", 2_147_997_852, 21_479_978.52m));
    }

    [Fact]
    public async Task Pagina_as_vendas_no_banco_das_mais_recentes_para_as_mais_antigas()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var ids = new List<int>();
        for (var dias = 4; dias >= 0; dias--)
        {
            ids.Add(await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], data: Hoje.AddDays(-dias)));
        }

        await using var contexto = banco.NovoContexto();
        var consultas = new ConsultasDeVendas(contexto);
        var primeira = await consultas.ListarPaginaAsync(null, null, MaisRecentes, 1, 2);
        var ultima = await consultas.ListarPaginaAsync(null, null, MaisRecentes, 3, 2);
        var alemDaUltima = await consultas.ListarPaginaAsync(null, null, MaisRecentes, 4, 2);

        primeira.Should().BeEquivalentTo(new { Numero = 1, Tamanho = 2, TotalDeItens = 5 });
        primeira.Itens.Select(v => v.Id).Should().Equal(ids[4], ids[3]);
        primeira.Itens[0].Should().Be(new VendaResumoDto(ids[4], Hoje, "529.982.247-25", "Maria Santos", 1, 2.50m));
        ultima.Itens.Select(v => v.Id).Should().Equal(ids[0]);
        alemDaUltima.Itens.Should().BeEmpty();
        alemDaUltima.TotalDeItens.Should().Be(5);
    }

    [Fact]
    public async Task A_pagina_de_vendas_e_contada_e_recortada_no_proprio_sql()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);
        var comandos = new List<string>();

        await using var contexto = banco.NovoContexto(comandos.Add);
        await new ConsultasDeVendas(contexto).ListarPaginaAsync(
            Hoje.AddDays(-7),
            Hoje,
            new Ordenacao<ColunaDeVendas>(ColunaDeVendas.Total, Decrescente: true),
            2,
            25);

        var sql = string.Join(Environment.NewLine, comandos);
        sql.Should().Contain("COUNT(*)").And.Contain("OFFSET").And.Contain("FETCH NEXT");
        sql.Should().Contain("[Data] >= @").And.Contain("[Data] <= @").And.Contain("SUM(");
    }

    [Fact]
    public async Task Filtra_o_periodo_e_ordena_pela_data_ou_pelo_total_no_banco()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);
        var antesDoPeriodo = await RegistrarVendaAsync(maria, [new(caneta, 10, 2.50m)], data: new DateOnly(2026, 11, 1));
        var dia5 = await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], data: new DateOnly(2026, 11, 5));
        var dia10 = await RegistrarVendaAsync(maria, [new(caderno, 2, 18.90m)], data: new DateOnly(2026, 11, 10));
        var dia12 = await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m), new(caderno, 1, 18.90m)], data: new DateOnly(2026, 11, 12));
        var dia5DeNovo = await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], data: new DateOnly(2026, 11, 5));

        await using var contexto = banco.NovoContexto();
        var consultas = new ConsultasDeVendas(contexto);
        var inicio = new DateOnly(2026, 11, 5);
        var fim = new DateOnly(2026, 11, 12);

        async Task<IEnumerable<int>> IdsAsync(DateOnly? de, DateOnly? ate, ColunaDeVendas coluna, bool decrescente) =>
            (await consultas.ListarPaginaAsync(de, ate, new(coluna, decrescente), 1, 10)).Itens.Select(v => v.Id);

        var maiores = await consultas.ListarPaginaAsync(inicio, fim, new(ColunaDeVendas.Total, Decrescente: true), 1, 10);
        maiores.TotalDeItens.Should().Be(4);
        maiores.Itens.Select(v => v.Id).Should().Equal(dia10, dia12, dia5DeNovo, dia5);
        maiores.Itens[1].Should().Be(
            new VendaResumoDto(dia12, new DateOnly(2026, 11, 12), "529.982.247-25", "Maria Santos", 2, 21.40m));
        (await IdsAsync(inicio, fim, ColunaDeVendas.Total, false)).Should().Equal(dia5, dia5DeNovo, dia12, dia10);
        (await IdsAsync(inicio, fim, ColunaDeVendas.Data, false)).Should().Equal(dia5, dia5DeNovo, dia10, dia12);
        (await IdsAsync(inicio, fim, ColunaDeVendas.Data, true)).Should().Equal(dia12, dia10, dia5DeNovo, dia5);
        (await IdsAsync(new DateOnly(2026, 11, 10), null, ColunaDeVendas.Data, true)).Should().Equal(dia12, dia10);
        (await IdsAsync(null, new DateOnly(2026, 11, 4), ColunaDeVendas.Data, true)).Should().Equal(antesDoPeriodo);
    }

    [Fact]
    public async Task Pagina_os_clientes_no_banco_pela_coluna_pedida_com_desempate_pelo_id()
    {
        var ana = await CadastrarClienteAsync("111.444.777-35", "Ana Lima");
        var bia = await CadastrarClienteAsync("529.982.247-25", "Bia Santos");
        var caio = await CadastrarClienteAsync("390.533.447-05", "Caio Lima");
        var outraAna = await CadastrarClienteAsync("200.000.001-08", "Ana Lima");

        await using var contexto = banco.NovoContexto();
        var repositorio = new ClienteRepositorio(contexto);
        var todos = BuscaDeCliente.Interpretar(null);

        async Task<IEnumerable<int>> IdsAsync(BuscaDeCliente busca, ColunaDeClientes coluna, bool decrescente, int numero, int tamanho) =>
            (await repositorio.ListarPaginaAsync(busca, new(coluna, decrescente), numero, tamanho)).Itens.Select(c => c.Id);

        (await IdsAsync(todos, ColunaDeClientes.Nome, false, 1, 3)).Should().Equal(ana, outraAna, bia);
        (await IdsAsync(todos, ColunaDeClientes.Nome, false, 2, 3)).Should().Equal(caio);
        (await IdsAsync(todos, ColunaDeClientes.Nome, true, 1, 4)).Should().Equal(caio, bia, outraAna, ana);
        (await IdsAsync(todos, ColunaDeClientes.Cpf, false, 1, 4)).Should().Equal(ana, outraAna, caio, bia);
        (await IdsAsync(todos, ColunaDeClientes.Cpf, true, 1, 4)).Should().Equal(bia, caio, outraAna, ana);

        var lima = await repositorio.ListarPaginaAsync(
            BuscaDeCliente.Interpretar("lima"),
            new(ColunaDeClientes.Nome, Decrescente: false),
            1,
            2);
        lima.TotalDeItens.Should().Be(3);
        lima.Itens.Select(c => c.Id).Should().Equal(ana, outraAna);
        (await IdsAsync(BuscaDeCliente.Interpretar("529.9"), ColunaDeClientes.Nome, false, 1, 25)).Should().Equal(bia);
    }

    [Fact]
    public async Task A_pagina_de_clientes_e_contada_ordenada_e_recortada_no_proprio_sql()
    {
        await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var comandos = new List<string>();

        await using var contexto = banco.NovoContexto(comandos.Add);
        await new ClienteRepositorio(contexto).ListarPaginaAsync(
            BuscaDeCliente.Interpretar("maria"),
            new Ordenacao<ColunaDeClientes>(ColunaDeClientes.Cpf, Decrescente: true),
            2,
            25);

        var sql = string.Join(Environment.NewLine, comandos);
        sql.Should().Contain("COUNT(*)").And.Contain("OFFSET").And.Contain("FETCH NEXT");
        sql.Should().Contain("LIKE").And.Contain("[Cpf] DESC");
    }

    [Fact]
    public async Task Pagina_os_produtos_no_banco_pelo_nome_ou_pelo_valor_com_desempate_pelo_id()
    {
        var borracha = await CadastrarProdutoAsync("Borracha", 1.50m);
        var regua = await CadastrarProdutoAsync("Régua", 4m);
        var apontador = await CadastrarProdutoAsync("Apontador", 1.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);

        await using var contexto = banco.NovoContexto();
        var repositorio = new ProdutoRepositorio(contexto);

        async Task<IEnumerable<int>> IdsAsync(string? trecho, ColunaDeProdutos coluna, bool decrescente, int numero, int tamanho) =>
            (await repositorio.ListarPaginaAsync(trecho, new(coluna, decrescente), numero, tamanho)).Itens.Select(p => p.Id);

        (await IdsAsync(null, ColunaDeProdutos.Nome, false, 1, 3)).Should().Equal(apontador, borracha, caderno);
        (await IdsAsync(null, ColunaDeProdutos.Nome, false, 2, 3)).Should().Equal(regua);
        (await IdsAsync(null, ColunaDeProdutos.Nome, true, 1, 4)).Should().Equal(regua, caderno, borracha, apontador);
        (await IdsAsync(null, ColunaDeProdutos.Valor, false, 1, 4)).Should().Equal(borracha, apontador, regua, caderno);
        (await IdsAsync(null, ColunaDeProdutos.Valor, true, 1, 4)).Should().Equal(caderno, regua, apontador, borracha);

        var comR = await repositorio.ListarPaginaAsync("r", new(ColunaDeProdutos.Valor, Decrescente: false), 1, 2);
        comR.TotalDeItens.Should().Be(4);
        comR.Itens.Select(p => p.Id).Should().Equal(borracha, apontador);
        (await IdsAsync("régua", ColunaDeProdutos.Nome, false, 1, 25)).Should().Equal(regua);
    }

    [Fact]
    public async Task A_pagina_de_produtos_e_contada_ordenada_e_recortada_no_proprio_sql()
    {
        await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var comandos = new List<string>();

        await using var contexto = banco.NovoContexto(comandos.Add);
        await new ProdutoRepositorio(contexto).ListarPaginaAsync(
            "caneta",
            new Ordenacao<ColunaDeProdutos>(ColunaDeProdutos.Valor, Decrescente: true),
            2,
            25);

        var sql = string.Join(Environment.NewLine, comandos);
        sql.Should().Contain("COUNT(*)").And.Contain("OFFSET").And.Contain("FETCH NEXT");
        sql.Should().Contain("LIKE").And.Contain("[ValorUnitario] DESC");
    }

    private async Task<int> CadastrarClienteAsync(string cpf, string nome, string? telefone = null)
    {
        await using var contexto = banco.NovoContexto();
        var cliente = Cliente.Criar(new DadosCliente(cpf, nome, null, null, telefone), Hoje).Valor;
        new ClienteRepositorio(contexto).Adicionar(cliente);
        await new UnidadeDeTrabalho(contexto).SalvarAsync();
        return cliente.Id;
    }

    private async Task<int> CadastrarProdutoAsync(string nome, decimal valor)
    {
        await using var contexto = banco.NovoContexto();
        var produto = Produto.Criar(new DadosProduto(nome, valor)).Valor;
        new ProdutoRepositorio(contexto).Adicionar(produto);
        await new UnidadeDeTrabalho(contexto).SalvarAsync();
        return produto.Id;
    }

    private async Task<int> RegistrarVendaAsync(
        int clienteId,
        IReadOnlyList<DadosItemVenda> itens,
        Guid? chave = null,
        DateOnly? data = null)
    {
        await using var contexto = banco.NovoContexto();
        var venda = Venda.Registrar(new DadosVenda(data ?? Hoje, clienteId, itens, chave), Hoje, new ParametrosDoSophia().QuantidadeMaximaPorItem).Valor;
        new VendaRepositorio(contexto).Adicionar(venda);
        await new UnidadeDeTrabalho(contexto).SalvarAsync();
        return venda.Id;
    }
}
