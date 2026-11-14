using System.ComponentModel.DataAnnotations;
using Sophia.Application.Produtos;
using Sophia.Domain.Produtos;

namespace Sophia.Web.Models;

public sealed class ProdutoFormulario
{
    public int? Id { get; set; }

    [Display(Name = "campo.nome")]
    [Required(ErrorMessage = "validacao.nome-obrigatorio")]
    [StringLength(Produto.TamanhoMaximoNome, ErrorMessage = "validacao.nome-tamanho")]
    public string? Nome { get; set; }

    [Display(Name = "campo.valor-unitario")]
    [Required(ErrorMessage = "validacao.valor-obrigatorio")]
    public decimal? ValorUnitario { get; set; }

    public bool EhNovo => Id is null;

    public static ProdutoFormulario De(ProdutoDto produto) => new()
    {
        Id = produto.Id,
        Nome = produto.Nome,
        ValorUnitario = produto.ValorUnitario,
    };

    public DadosProduto ParaDados() => new(Nome, ValorUnitario);
}
