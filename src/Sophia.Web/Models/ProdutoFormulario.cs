using System.ComponentModel.DataAnnotations;
using Sophia.Application;
using Sophia.Application.Produtos;
using Sophia.Domain.Produtos;
using Sophia.Web.Apoio;

namespace Sophia.Web.Models;

public sealed class ProdutoFormulario : IFormularioDeCadastro<DadosProduto>
{
    public int? Id { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessageResourceType = typeof(RegrasDeEntrada), ErrorMessageResourceName = nameof(RegrasDeEntrada.NomeObrigatorio))]
    [TamanhoDoNome(Produto.TamanhoMaximoNome)]
    public string? Nome { get; set; }

    [Display(Name = "Valor unitário")]
    [Required(ErrorMessageResourceType = typeof(RegrasDeEntrada), ErrorMessageResourceName = nameof(RegrasDeEntrada.ValorUnitarioObrigatorio))]
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
