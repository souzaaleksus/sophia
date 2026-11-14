using Reqnroll;
using Sophia.Application.Clientes;
using Sophia.Domain.Clientes;
using Sophia.Specs.Apoio;

namespace Sophia.Specs.Passos;

[Binding]
public sealed class PassosDeClientes(Cenario cenario)
{
    private IReadOnlyList<ClienteDto> _encontrados = [];

    [When("eu cadastrar o cliente")]
    public async Task QuandoEuCadastrarOCliente(DataTable tabela)
    {
        var linha = tabela.Rows.Single();
        var dados = new DadosCliente(
            Cenario.Texto(linha["Cpf"]),
            Cenario.Texto(linha["Nome"]),
            Cenario.Data(linha["DataNascimento"]),
            Cenario.Texto(linha["Email"]),
            Cenario.Texto(linha["Telefone"]));

        cenario.UltimoResultado = await cenario.Clientes.CadastrarAsync(dados);
    }

    [When("eu alterar o nome do cliente {string} para {string}")]
    public async Task QuandoEuAlterarONome(string nomeAtual, string novoNome)
    {
        var cliente = await cenario.ClienteChamadoAsync(nomeAtual);
        cenario.UltimoResultado = await cenario.Clientes.AlterarAsync(cliente.Id, ComoDados(cliente) with { Nome = novoNome });
    }

    [When("eu alterar o CPF do cliente {string} para {string}")]
    public async Task QuandoEuAlterarOCpf(string nome, string novoCpf)
    {
        var cliente = await cenario.ClienteChamadoAsync(nome);
        cenario.UltimoResultado = await cenario.Clientes.AlterarAsync(cliente.Id, ComoDados(cliente) with { Cpf = novoCpf });
    }

    [When("eu buscar clientes por {string}")]
    public async Task QuandoEuBuscarClientes(string termo)
    {
        _encontrados = await cenario.Clientes.ListarAsync(termo);
    }

    [When("eu excluir o cliente {string}")]
    public async Task QuandoEuExcluirOCliente(string nome)
    {
        var cliente = await cenario.ClienteChamadoAsync(nome);
        cenario.UltimoResultado = await cenario.Clientes.ExcluirAsync(cliente.Id);
    }

    [Then("o cliente {string} aparece na lista com o CPF {string}")]
    public async Task EntaoOClienteApareceNaLista(string nome, string cpf)
    {
        var clientes = await cenario.Clientes.ListarAsync(null);
        clientes.Should().ContainSingle(c => c.Nome == nome).Which.Cpf.Should().Be(cpf);
    }

    [Then("encontro somente o cliente {string}")]
    public void EntaoEncontroSomenteOCliente(string nome)
    {
        _encontrados.Select(c => c.Nome).Should().Equal(nome);
    }

    [Then("a lista de clientes fica vazia")]
    public async Task EntaoAListaDeClientesFicaVazia()
    {
        (await cenario.Clientes.ListarAsync(null)).Should().BeEmpty();
    }

    private static DadosCliente ComoDados(ClienteDto cliente) =>
        new(cliente.Cpf, cliente.Nome, cliente.DataNascimento, cliente.Email, cliente.Telefone);
}
