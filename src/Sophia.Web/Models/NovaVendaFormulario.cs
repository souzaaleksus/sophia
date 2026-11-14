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

    public int? Quantidade { get; set; }

    public decimal? ValorUnitario { get; set; }
}
