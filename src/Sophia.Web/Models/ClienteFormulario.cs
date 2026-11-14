using System.ComponentModel.DataAnnotations;
using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;
using Sophia.Web.Apoio;

namespace Sophia.Web.Models;

public sealed class ClienteFormulario : IFormularioDeCadastro<DadosCliente>
{
    public int? Id { get; set; }

    [Display(Name = "campo.cpf")]
    [Required(ErrorMessage = "validacao.cpf-obrigatorio")]
    public string? Cpf { get; set; }

    [Display(Name = "campo.nome")]
    [Required(ErrorMessage = "validacao.nome-obrigatorio")]
    [StringLength(Cliente.TamanhoMaximoNome, ErrorMessage = "validacao.nome-tamanho")]
    public string? Nome { get; set; }

    [Display(Name = "campo.data-de-nascimento")]
    [DataBrasileira(ErrorMessage = "validacao.nascimento-invalido")]
    public string? DataNascimento { get; set; }

    [Display(Name = "campo.email")]
    [EmailAddress(ErrorMessage = "validacao.email-invalido")]
    [StringLength(Cliente.TamanhoMaximoEmail, ErrorMessage = "validacao.email-invalido")]
    public string? Email { get; set; }

    [Display(Name = "campo.telefone")]
    public string? Telefone { get; set; }

    public bool EhNovo => Id is null;

    public static ClienteFormulario De(ClienteDto cliente) => new()
    {
        Id = cliente.Id,
        Cpf = cliente.Cpf,
        Nome = cliente.Nome,
        DataNascimento = Formato.Data(cliente.DataNascimento),
        Email = cliente.Email,
        Telefone = cliente.Telefone,
    };

    public DadosCliente ParaDados() => new(Cpf, Nome, DataBrasileiraAttribute.Ler(DataNascimento), Email, Telefone);
}
