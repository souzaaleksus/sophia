using System.ComponentModel.DataAnnotations;
using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;
using Sophia.Domain.Recursos;
using Sophia.Web.Apoio;

namespace Sophia.Web.Models;

public sealed class ClienteFormulario : IFormularioDeCadastro<DadosCliente>
{
    public int? Id { get; set; }

    [Display(Name = "CPF")]
    [Required(ErrorMessageResourceType = typeof(Mensagens), ErrorMessageResourceName = nameof(Mensagens.CpfObrigatorio))]
    public string? Cpf { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessageResourceType = typeof(Mensagens), ErrorMessageResourceName = nameof(Mensagens.NomeObrigatorio))]
    [TamanhoDoNome(Cliente.TamanhoMaximoNome)]
    public string? Nome { get; set; }

    [Display(Name = "Data de nascimento")]
    [DataBrasileira(ErrorMessageResourceType = typeof(Mensagens), ErrorMessageResourceName = nameof(Mensagens.DataDeNascimentoInvalida))]
    public string? DataNascimento { get; set; }

    [Display(Name = "E-mail")]
    [EmailAddress(ErrorMessageResourceType = typeof(Mensagens), ErrorMessageResourceName = nameof(Mensagens.EmailInvalido))]
    [StringLength(Cliente.TamanhoMaximoEmail, ErrorMessageResourceType = typeof(Mensagens), ErrorMessageResourceName = nameof(Mensagens.EmailInvalido))]
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
