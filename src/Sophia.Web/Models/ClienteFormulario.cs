using System.ComponentModel.DataAnnotations;
using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;
using Sophia.Web.Apoio;

namespace Sophia.Web.Models;

public sealed class ClienteFormulario
{
    public int? Id { get; set; }

    [Display(Name = "CPF")]
    [Required(ErrorMessage = "Informe o CPF.")]
    public string? Cpf { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(Cliente.TamanhoMaximoNome, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
    public string? Nome { get; set; }

    [Display(Name = "Data de nascimento")]
    [DataBrasileira(ErrorMessage = "Informe uma data de nascimento válida.")]
    public string? DataNascimento { get; set; }

    [Display(Name = "E-mail")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(Cliente.TamanhoMaximoEmail, ErrorMessage = "E-mail inválido.")]
    public string? Email { get; set; }

    [Display(Name = "Telefone")]
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
