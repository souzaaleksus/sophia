namespace Sophia.Web.Models;

public interface IFormularioDeCadastro<out TDados>
{
    int? Id { get; set; }

    TDados ParaDados();
}
