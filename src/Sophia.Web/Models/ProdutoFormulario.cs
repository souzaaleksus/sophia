using System.ComponentModel.DataAnnotations;
using Sophia.Application.Produtos;
using Sophia.Domain.Produtos;

namespace Sophia.Web.Models;

public sealed class ProdutoFormulario
{
    public int? Id { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(Produto.TamanhoMaximoNome, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
    public string? Nome { get; set; }

    [Display(Name = "Valor unitário")]
    [Required(ErrorMessage = "Informe o valor unitário.")]
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
