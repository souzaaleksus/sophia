using Sophia.Application;
using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;
using Sophia.Web.Apoio;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class ClientesController(
    IServicoDeCadastro<ClienteDto, DadosCliente> clientes,
    ParametrosDoSophia parametros)
    : CadastroController<ClienteDto, ClienteFormulario, DadosCliente>(
        clientes,
        new("Cliente cadastrado.", "Cliente alterado.", "Cliente excluído."),
        parametros)
{
    protected override ClienteFormulario ParaFormulario(ClienteDto registro) => ClienteFormulario.De(registro);

    protected override object Sugestao(ClienteDto registro) => new
    {
        registro.Id,
        registro.Cpf,
        registro.Nome,
        Iniciais = Formato.Iniciais(registro.Nome),
        TomDoAvatar = Formato.TomDoAvatar(registro.Nome),
    };
}
