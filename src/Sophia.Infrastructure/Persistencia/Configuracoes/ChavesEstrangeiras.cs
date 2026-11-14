using System.Collections.Frozen;

namespace Sophia.Infrastructure.Persistencia.Configuracoes;

internal static class ChavesEstrangeiras
{
    public const string VendaDoCliente = "FK_Vendas_Clientes";
    public const string ItemDaVenda = "FK_VendaItens_Vendas";
    public const string ProdutoDoItem = "FK_VendaItens_Produtos";

    public static FrozenSet<string> Todas { get; } =
        new[] { VendaDoCliente, ItemDaVenda, ProdutoDoItem }.ToFrozenSet(StringComparer.Ordinal);
}
