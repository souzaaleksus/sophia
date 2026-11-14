using System.ComponentModel.DataAnnotations;
using Sophia.Domain.Vendas;

namespace Sophia.Web.Models;

public sealed class NovaVendaFormulario
{
    public Guid? Chave { get; set; }

    [Display(Name = "campo.data-da-venda")]
    public DateOnly? Data { get; set; }

    public int? ClienteId { get; set; }

    public string? ClienteNome { get; set; }

    public string? ClienteCpf { get; set; }

    public List<ItemDaVendaFormulario> Itens { get; set; } = [];

    public IEnumerable<ItemDaVendaFormulario> Lancados => Itens.Where(item => item.ProdutoId is not null);

    public long UnidadesLancadas() => Lancados.Sum(item => (long)(item.Quantidade ?? 0));

    public decimal? Total()
    {
        try
        {
            return Itens.Aggregate((decimal?)0m, (soma, item) => soma + item.Subtotal());
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    public DadosVenda ParaDados() => new(
        Data,
        ClienteId,
        Itens.Select(i => new DadosItemVenda(i.ProdutoId, i.Quantidade, i.ValorUnitario)).ToList(),
        Chave);
}

public sealed class ItemDaVendaFormulario
{
    public int? ProdutoId { get; set; }

    public string? ProdutoNome { get; set; }

    [Display(Name = "campo.quantidade")]
    public int? Quantidade { get; set; }

    [Display(Name = "campo.valor-unitario")]
    public decimal? ValorUnitario { get; set; }

    public decimal? Subtotal()
    {
        try
        {
            return (Quantidade ?? 0) * (ValorUnitario ?? 0);
        }
        catch (OverflowException)
        {
            return null;
        }
    }
}
