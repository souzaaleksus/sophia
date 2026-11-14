using Sophia.Application;
using Sophia.Application.Vendas;
using Sophia.Integration.Tests.Apoio;
using Sophia.Testes.Banco;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class CpfGravadoForaDaRegraTests(BancoDeTestes banco) : IAsyncLifetime, IDisposable
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    private readonly SistemaEmTeste _sistema = new(banco, Hoje);

    public async Task InitializeAsync()
    {
        await banco.LimparAsync();
        await banco.ExecutarAsync("""
            INSERT INTO dbo.Clientes (Cpf, Nome) VALUES ('11111111111', N'Cliente Repetido'), ('52998224724', N'Cliente Digitado Errado');
            INSERT INTO dbo.Produtos (Nome, ValorUnitario) VALUES (N'Caneta azul', 2.50);
            INSERT INTO dbo.Vendas (Data, ClienteId) VALUES ('2026-11-13', 2);
            INSERT INTO dbo.VendaItens (VendaId, ProdutoId, Quantidade, ValorUnitario) VALUES (1, 1, 2, 2.50);
            """);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _sistema.Dispose();

    [Fact]
    public async Task A_lista_de_clientes_mostra_quem_tem_cpf_fora_da_regra()
    {
        var lista = await _sistema.Clientes.ListarAsync(new ConsultaDeLista());

        lista.Pagina.Itens.Select(c => (c.Nome, c.Cpf)).Should().Equal(
            ("Cliente Digitado Errado", "529.982.247-24"),
            ("Cliente Repetido", "111.111.111-11"));
    }

    [Fact]
    public async Task A_busca_e_o_cadastro_leem_o_cliente_com_cpf_fora_da_regra()
    {
        (await _sistema.Clientes.BuscarAsync("111", 8)).Select(c => c.Nome).Should().Equal("Cliente Repetido");
        (await _sistema.Clientes.BuscarAsync("errado", 8)).Select(c => c.Cpf).Should().Equal("529.982.247-24");
        (await _sistema.Clientes.ObterAsync(1))!.Cpf.Should().Be("111.111.111-11");
    }

    [Fact]
    public async Task O_painel_e_as_vendas_leem_a_venda_do_cliente_com_cpf_fora_da_regra()
    {
        var painel = await _sistema.Painel.ObterAsync();
        var vendas = await _sistema.Vendas.ListarAsync(new ConsultaDeVendas());
        var detalhe = await _sistema.Vendas.ObterAsync(1);

        painel.UltimasVendas.Should().ContainSingle().Which.ClienteCpf.Should().Be("529.982.247-24");
        vendas.Pagina.Itens.Should().ContainSingle().Which.ClienteNome.Should().Be("Cliente Digitado Errado");
        detalhe!.ClienteCpf.Should().Be("529.982.247-24");
    }
}
