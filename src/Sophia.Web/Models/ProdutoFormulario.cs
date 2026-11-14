using System.ComponentModel.DataAnnotations;
using Sophia.Application.Produtos;
using Sophia.Domain.Produtos;
using Sophia.Domain.Recursos;
using Sophia.Web.Apoio;

namespace Sophia.Web.Models;

public sealed class ProdutoFormulario : IFormularioDeCadastro<DadosProduto>
{
    public int? Id { get; set; }

    [Display(Name = "campo.nome")]
    [Required(ErrorMessageResourceType = typeof(Mensagens), ErrorMessageResourceName = nameof(Mensagens.NomeObrigatorio))]
    [TamanhoDoNome(Produto.TamanhoMaximoNome)]
    public string? Nome { get; set; }

    [Display(Name = "campo.valor-unitario")]
    [Required(ErrorMessageResourceType = typeof(Mensagens), ErrorMessageResourceName = nameof(Mensagens.ValorUnitarioObrigatorio))]
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
