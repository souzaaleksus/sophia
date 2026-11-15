using Sophia.Domain.Recursos;

namespace Sophia.Domain.Vendas;

public sealed class Venda
{
    public const int QuantidadeMaximaAceita = 999_999;

    private readonly List<ItemVenda> _itens = [];

    private Venda()
    {
    }

    public int Id { get; }

    public DateOnly Data { get; private set; }

    public int ClienteId { get; private set; }

    public Guid? ChaveDeRegistro { get; private set; }

    public IReadOnlyCollection<ItemVenda> Itens => _itens.AsReadOnly();

    public decimal Total => _itens.Sum(item => item.Subtotal);

    public bool TemOMesmoConteudoQue(Venda outra) =>
        ClienteId == outra.ClienteId
        && Data == outra.Data
        && ConteudoDosItens().SetEquals(outra.ConteudoDosItens());

    private HashSet<(int ProdutoId, int Quantidade, decimal ValorUnitario)> ConteudoDosItens() =>
        _itens.Select(item => (item.ProdutoId, item.Quantidade, item.ValorUnitario)).ToHashSet();

    public static Resultado<Venda> Registrar(
        DadosVenda dados,
        DateOnly hoje,
        int quantidadeMaximaPorItem,
        IReadOnlyDictionary<int, string> nomesDosProdutos)
    {
        ExigirTetoNaFaixa(quantidadeMaximaPorItem);
        var erros = new List<Erro>();
        ValidarData(dados.Data, hoje, erros);
        ValidarCliente(dados.ClienteId, erros);
        var itens = ValidarItens(dados.Itens, new RegrasDosItens(quantidadeMaximaPorItem, nomesDosProdutos), erros);

        if (erros.Count > 0)
        {
            return Resultado.Falha<Venda>(erros);
        }

        var venda = new Venda
        {
            Data = dados.Data!.Value,
            ClienteId = dados.ClienteId!.Value,
            ChaveDeRegistro = dados.Chave == Guid.Empty ? null : dados.Chave,
        };
        venda._itens.AddRange(itens);
        return Resultado.Ok(venda);
    }

    private static void ExigirTetoNaFaixa(int quantidadeMaximaPorItem)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantidadeMaximaPorItem, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantidadeMaximaPorItem, QuantidadeMaximaAceita);
    }

    private static void ValidarData(DateOnly? data, DateOnly hoje, List<Erro> erros)
    {
        if (data is null)
        {
            erros.Add(new Erro(nameof(DadosVenda.Data), Mensagens.DataDaVendaObrigatoria));
        }
        else if (data > hoje)
        {
            erros.Add(new Erro(nameof(DadosVenda.Data), Mensagens.DataDaVendaNoFuturo));
        }
    }

    private static void ValidarCliente(int? clienteId, List<Erro> erros)
    {
        if (clienteId is null or <= 0)
        {
            erros.Add(new Erro(nameof(DadosVenda.ClienteId), Mensagens.ClienteNaoSelecionado));
        }
    }

    private static List<ItemVenda> ValidarItens(IReadOnlyList<DadosItemVenda> itens, RegrasDosItens regras, List<Erro> erros)
    {
        if (itens.Count == 0)
        {
            erros.Add(new Erro(nameof(DadosVenda.Itens), Mensagens.VendaSemItens));
            return [];
        }

        var validos = new List<ItemVenda>();
        var produtosLancados = new HashSet<int>();
        for (var linha = 0; linha < itens.Count; linha++)
        {
            var item = ValidarItem(itens[linha], new LinhaDaVenda(linha, regras, produtosLancados), erros);
            if (item is not null)
            {
                validos.Add(item);
            }
        }

        return validos;
    }

    private static ItemVenda? ValidarItem(DadosItemVenda dados, LinhaDaVenda linha, List<Erro> erros)
    {
        var errosAntes = erros.Count;
        var nomeDoProduto = ValidarProduto(dados.ProdutoId, linha, erros);
        ValidarQuantidade(dados.Quantidade, linha, erros);
        var valorUnitario = RegrasComuns.ValidarValorUnitario(
            dados.ValorUnitario, nameof(DadosItemVenda.ValorUnitario), linha.Numero, erros);

        return erros.Count == errosAntes
            ? new ItemVenda(dados.ProdutoId!.Value, nomeDoProduto!, dados.Quantidade!.Value, valorUnitario!.Value)
            : null;
    }

    private static string? ValidarProduto(int? produtoId, LinhaDaVenda linha, List<Erro> erros)
    {
        string? nome = null;
        string? problema = produtoId switch
        {
            null or <= 0 => Mensagens.ProdutoNaoSelecionado,
            _ when !linha.ProdutosLancados.Add(produtoId.Value) => Mensagens.ProdutoRepetidoNaVenda,
            _ when !linha.Regras.NomesDosProdutos.TryGetValue(produtoId.Value, out nome) => Mensagens.ProdutoNaoEncontrado,
            _ => null,
        };

        if (problema is not null)
        {
            erros.Add(new Erro(nameof(DadosItemVenda.ProdutoId), problema, linha.Numero));
        }

        return nome;
    }

    private static void ValidarQuantidade(int? quantidade, LinhaDaVenda linha, List<Erro> erros)
    {
        string? problema = quantidade switch
        {
            null => Mensagens.QuantidadeObrigatoria,
            <= 0 => Mensagens.QuantidadeNaoPositiva,
            _ when quantidade > linha.Regras.QuantidadeMaxima => Mensagens.QuantidadeAcimaDoMaximo(linha.Regras.QuantidadeMaxima),
            _ => null,
        };

        if (problema is not null)
        {
            erros.Add(new Erro(nameof(DadosItemVenda.Quantidade), problema, linha.Numero));
        }
    }

    private sealed record RegrasDosItens(int QuantidadeMaxima, IReadOnlyDictionary<int, string> NomesDosProdutos);

    private sealed record LinhaDaVenda(int Numero, RegrasDosItens Regras, HashSet<int> ProdutosLancados);
}
