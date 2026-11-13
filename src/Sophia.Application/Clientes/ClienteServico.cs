using Sophia.Application.Abstracoes;
using Sophia.Domain;
using Sophia.Domain.Clientes;

namespace Sophia.Application.Clientes;

public sealed class ClienteServico(
    IClienteRepositorio clientes,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IRelogio relogio) : IClienteServico
{
    private static readonly Erro CpfDuplicado = new("Cpf", "Já existe um cliente com este CPF.");
    private static readonly Erro NaoEncontrado = new(string.Empty, "Cliente não encontrado.");
    private static readonly Erro PossuiVendas = new(string.Empty, "Este cliente possui vendas e não pode ser excluído.");

    public async Task<IReadOnlyList<ClienteDto>> ListarAsync(
        string? termo,
        int? limite = null,
        CancellationToken cancelamento = default)
    {
        var encontrados = await clientes.ListarAsync(BuscaDeCliente.Interpretar(termo), limite, cancelamento);
        return encontrados.Select(ClienteDto.De).ToList();
    }

    public async Task<ClienteDto?> ObterAsync(int id, CancellationToken cancelamento = default)
    {
        var cliente = await clientes.ObterAsync(id, cancelamento);
        return cliente is null ? null : ClienteDto.De(cliente);
    }

    public async Task<Resultado<int>> CadastrarAsync(DadosCliente dados, CancellationToken cancelamento = default)
    {
        var criacao = Cliente.Criar(dados, relogio.Hoje);
        if (!criacao.Sucesso)
        {
            return Resultado.Falha<int>(criacao.Erros);
        }

        var cliente = criacao.Valor;
        if (await clientes.ExisteComCpfAsync(cliente.Cpf, excetoId: null, cancelamento))
        {
            return Resultado.Falha<int>(CpfDuplicado);
        }

        clientes.Adicionar(cliente);
        var gravacao = await SalvarAsync(cancelamento);
        return gravacao.Sucesso ? Resultado.Ok(cliente.Id) : Resultado.Falha<int>(gravacao.Erros);
    }

    public async Task<Resultado> AlterarAsync(int id, DadosCliente dados, CancellationToken cancelamento = default)
    {
        var cliente = await clientes.ObterAsync(id, cancelamento);
        if (cliente is null)
        {
            return Resultado.Falha(NaoEncontrado);
        }

        var alteracao = cliente.Alterar(dados, relogio.Hoje);
        if (!alteracao.Sucesso)
        {
            return alteracao;
        }

        if (await clientes.ExisteComCpfAsync(cliente.Cpf, excetoId: id, cancelamento))
        {
            return Resultado.Falha(CpfDuplicado);
        }

        return await SalvarAsync(cancelamento);
    }

    public async Task<Resultado> ExcluirAsync(int id, CancellationToken cancelamento = default)
    {
        var cliente = await clientes.ObterAsync(id, cancelamento);
        if (cliente is null)
        {
            return Resultado.Falha(NaoEncontrado);
        }

        if (await clientes.PossuiVendasAsync(id, cancelamento))
        {
            return Resultado.Falha(PossuiVendas);
        }

        clientes.Remover(cliente);
        return await SalvarAsync(cancelamento);
    }

    private async Task<Resultado> SalvarAsync(CancellationToken cancelamento)
    {
        try
        {
            await unidadeDeTrabalho.SalvarAsync(cancelamento);
            return Resultado.Ok();
        }
        catch (ConflitoAoSalvarException conflito)
        {
            return Resultado.Falha(conflito.Tipo == TipoDeConflito.ValorDuplicado ? CpfDuplicado : PossuiVendas);
        }
    }
}
