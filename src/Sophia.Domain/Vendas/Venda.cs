namespace Sophia.Domain.Vendas;

public sealed class Venda
{
    private const string MensagemProdutoRepetido =
        "Este produto já foi lançado nesta venda; ajuste a quantidade na linha anterior.";

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

    public static Resultado<Venda> Registrar(DadosVenda dados, DateOnly hoje)
    {
        var erros = new List<Erro>();
        ValidarData(dados.Data, hoje, erros);
        ValidarCliente(dados.ClienteId, erros);
        var itens = ValidarItens(dados.Itens, erros);

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

    private static void ValidarData(DateOnly? data, DateOnly hoje, List<Erro> erros)
    {
        if (data is null)
        {
            erros.Add(new Erro("Data", "Informe a data da venda."));
        }
        else if (data > hoje)
        {
            erros.Add(new Erro("Data", "A data da venda não pode estar no futuro."));
        }
    }

    private static void ValidarCliente(int? clienteId, List<Erro> erros)
    {
        if (clienteId is null or <= 0)
        {
            erros.Add(new Erro("ClienteId", "Selecione o cliente."));
        }
    }

    private static List<ItemVenda> ValidarItens(IReadOnlyList<DadosItemVenda> itens, List<Erro> erros)
    {
        if (itens.Count == 0)
        {
            erros.Add(new Erro("Itens", "Lance ao menos um produto na venda."));
            return [];
        }

        var validos = new List<ItemVenda>();
        var produtosLancados = new HashSet<int>();
        for (var linha = 0; linha < itens.Count; linha++)
        {
            var item = ValidarItem(itens[linha], $"Itens[{linha}]", produtosLancados, erros);
            if (item is not null)
            {
                validos.Add(item);
            }
        }

        return validos;
    }

    private static ItemVenda? ValidarItem(
        DadosItemVenda dados,
        string linha,
        HashSet<int> produtosLancados,
        List<Erro> erros)
    {
        var errosAntes = erros.Count;
        ValidarProduto(dados.ProdutoId, linha, produtosLancados, erros);
        ValidarQuantidade(dados.Quantidade, linha, erros);
        var valorUnitario = RegrasComuns.ValidarValorUnitario(dados.ValorUnitario, $"{linha}.ValorUnitario", erros);

        return erros.Count == errosAntes
            ? new ItemVenda(dados.ProdutoId!.Value, dados.Quantidade!.Value, valorUnitario!.Value)
            : null;
    }

    private static void ValidarProduto(int? produtoId, string linha, HashSet<int> produtosLancados, List<Erro> erros)
    {
        if (produtoId is null or <= 0)
        {
            erros.Add(new Erro($"{linha}.ProdutoId", "Selecione o produto."));
        }
        else if (!produtosLancados.Add(produtoId.Value))
        {
            erros.Add(new Erro($"{linha}.ProdutoId", MensagemProdutoRepetido));
        }
    }

    private static void ValidarQuantidade(int? quantidade, string linha, List<Erro> erros)
    {
        if (quantidade is null)
        {
            erros.Add(new Erro($"{linha}.Quantidade", "Informe a quantidade."));
        }
        else if (quantidade <= 0)
        {
            erros.Add(new Erro($"{linha}.Quantidade", "A quantidade deve ser maior que zero."));
        }
    }
}
