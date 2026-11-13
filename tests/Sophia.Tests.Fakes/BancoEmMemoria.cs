using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;

namespace Sophia.Tests.Fakes;

public sealed class BancoEmMemoria : IUnidadeDeTrabalho
{
    private bool _simularConcorrencia;

    public BancoEmMemoria()
    {
        Clientes = new ClienteRepositorioEmMemoria(this);
        Produtos = new ProdutoRepositorioEmMemoria(this);
        Vendas = new VendaRepositorioEmMemoria(this);
    }

    public IClienteRepositorio Clientes { get; }

    public IProdutoRepositorio Produtos { get; }

    public IVendaRepositorio Vendas { get; }

    internal Tabela<Cliente> TabelaDeClientes { get; } = new();

    internal Tabela<Produto> TabelaDeProdutos { get; } = new();

    internal Tabela<Venda> TabelaDeVendas { get; } = new();

    public void SimularCadastroConcorrenteNoProximoSalvamento() => _simularConcorrencia = true;

    public void RegistrarVendaPara(int clienteId) => RegistrarVenda(clienteId, produtoId: int.MaxValue);

    public void RegistrarVendaDoProduto(int produtoId) => RegistrarVenda(clienteId: int.MaxValue, produtoId);

    public Task SalvarAsync(CancellationToken cancelamento = default)
    {
        if (_simularConcorrencia)
        {
            _simularConcorrencia = false;
            DescartarPendencias();
            throw new ConflitoAoSalvarException(TipoDeConflito.ValorDuplicado);
        }

        if (ViolaUnicidade())
        {
            DescartarPendencias();
            throw new ConflitoAoSalvarException(TipoDeConflito.ValorDuplicado);
        }

        if (RemoveRegistroEmUso())
        {
            DescartarPendencias();
            throw new ConflitoAoSalvarException(TipoDeConflito.RegistroEmUso);
        }

        TabelaDeClientes.Confirmar();
        TabelaDeProdutos.Confirmar();
        TabelaDeVendas.Confirmar(NumerarItens);
        return Task.CompletedTask;
    }

    private static void NumerarItens(Venda venda, int vendaId)
    {
        foreach (var item in venda.Itens)
        {
            typeof(ItemVenda).GetProperty(nameof(ItemVenda.VendaId))!.SetValue(item, vendaId);
        }
    }

    private bool ViolaUnicidade()
    {
        var cpfs = TabelaDeClientes.EstadoAposSalvar().Select(c => c.Cpf.Numero);
        var nomes = TabelaDeProdutos.EstadoAposSalvar().Select(p => p.Nome.ToUpperInvariant());
        return cpfs.Count() != cpfs.Distinct().Count() || nomes.Count() != nomes.Distinct().Count();
    }

    private bool RemoveRegistroEmUso()
    {
        var vendas = TabelaDeVendas.Gravados;
        return TabelaDeClientes.Removidos().Any(c => vendas.Any(v => v.ClienteId == c.Id))
            || TabelaDeProdutos.Removidos().Any(p => vendas.Any(v => v.Itens.Any(i => i.ProdutoId == p.Id)));
    }

    private void DescartarPendencias()
    {
        TabelaDeClientes.Descartar();
        TabelaDeProdutos.Descartar();
        TabelaDeVendas.Descartar();
    }

    private void RegistrarVenda(int clienteId, int produtoId)
    {
        var hoje = new DateOnly(2026, 11, 13);
        var venda = Venda.Registrar(new DadosVenda(hoje, clienteId, [new(produtoId, 1, 1m)]), hoje).Valor;
        TabelaDeVendas.Adicionar(venda);
        TabelaDeVendas.Confirmar(NumerarItens);
    }
}
