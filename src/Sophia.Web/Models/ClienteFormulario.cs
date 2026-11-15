using System.ComponentModel.DataAnnotations;
using Sophia.Application;
using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;
using Sophia.Web.Apoio;

namespace Sophia.Web.Models;

public sealed class ClienteFormulario : IFormularioDeCadastro<DadosCliente>
{
    public int? Id { get; set; }

    [Display(Name = "CPF")]
    [Required(ErrorMessageResourceType = typeof(RegrasDeEntrada), ErrorMessageResourceName = nameof(RegrasDeEntrada.CpfObrigatorio))]
    public string? Cpf { get; set; }

    [Display(Name = "Nome")]
    [Required(ErrorMessageResourceType = typeof(RegrasDeEntrada), ErrorMessageResourceName = nameof(RegrasDeEntrada.NomeObrigatorio))]
    [TamanhoDoNome(Cliente.TamanhoMaximoNome)]
    public string? Nome { get; set; }

    [Display(Name = "Data de nascimento")]
    [DataBrasileira(ErrorMessageResourceType = typeof(RegrasDeEntrada), ErrorMessageResourceName = nameof(RegrasDeEntrada.DataDeNascimentoInvalida))]
    public string? DataNascimento { get; set; }

    [Display(Name = "E-mail")]
    [EmailAddress(ErrorMessageResourceType = typeof(RegrasDeEntrada), ErrorMessageResourceName = nameof(RegrasDeEntrada.EmailInvalido))]
    [StringLength(Cliente.TamanhoMaximoEmail, ErrorMessageResourceType = typeof(RegrasDeEntrada), ErrorMessageResourceName = nameof(RegrasDeEntrada.EmailInvalido))]
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
