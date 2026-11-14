using Microsoft.Extensions.Localization;
using Sophia.Application;
using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;
using Sophia.Web.Models;

namespace Sophia.Web.Controllers;

public sealed class ClientesController(IClienteServico clientes, ParametrosDoSophia parametros, IStringLocalizer<Textos> textos)
    : CadastroController<ClienteDto, ClienteFormulario, DadosCliente>(
        new(clientes.ListarAsync, clientes.BuscarAsync, clientes.ObterAsync, clientes.CadastrarAsync, clientes.AlterarAsync, clientes.ExcluirAsync),
        new(textos["clientes.cadastrado"], textos["clientes.alterado"], textos["clientes.excluido"]),
        parametros)
{
    protected override ClienteFormulario ParaFormulario(ClienteDto registro) => ClienteFormulario.De(registro);

    protected override object Sugestao(ClienteDto registro) => new { registro.Id, registro.Cpf, registro.Nome };
}
