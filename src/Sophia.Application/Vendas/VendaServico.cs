using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Domain;
using Sophia.Domain.Vendas;

namespace Sophia.Application.Vendas;

public sealed class VendaServico(
    IVendaRepositorio vendas,
    IClienteRepositorio clientes,
    IProdutoRepositorio produtos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IRelogio relogio) : IVendaServico
{
    public DateOnly DataSugerida() => relogio.Hoje;

    public Task<IReadOnlyList<VendaResumoDto>> ListarAsync(CancellationToken cancelamento = default) =>
        vendas.ListarAsync(cancelamento: cancelamento);

    public Task<VendaDetalheDto?> ObterAsync(int id, CancellationToken cancelamento = default) =>
        vendas.ObterDetalheAsync(id, cancelamento);

    public async Task<Resultado<int>> RegistrarAsync(DadosVenda dados, CancellationToken cancelamento = default)
    {
        var registro = Venda.Registrar(dados, relogio.Hoje);
        if (!registro.Sucesso)
        {
            return Resultado.Falha<int>(registro.Erros);
        }

        var venda = registro.Valor;
        if (await JaRegistradaAsync(venda, cancelamento) is { } existente)
        {
            return Resultado.Ok(existente);
        }

        var erros = await VerificarCadastrosAsync(venda, cancelamento);
        if (erros.Count > 0)
        {
            return Resultado.Falha<int>(erros);
        }

        vendas.Adicionar(venda);
        try
        {
            await unidadeDeTrabalho.SalvarAsync(cancelamento);
        }
        catch (ConflitoAoSalvarException conflito) when (conflito.Tipo == TipoDeConflito.ValorDuplicado)
        {
            if (await JaRegistradaAsync(venda, cancelamento) is { } vencedora)
            {
                return Resultado.Ok(vencedora);
            }

            throw;
        }

        return Resultado.Ok(venda.Id);
    }

    private async Task<int?> JaRegistradaAsync(Venda venda, CancellationToken cancelamento) =>
        venda.ChaveDeRegistro is { } chave ? await vendas.ObterIdPelaChaveAsync(chave, cancelamento) : null;

    private async Task<List<Erro>> VerificarCadastrosAsync(Venda venda, CancellationToken cancelamento)
    {
        var erros = new List<Erro>();
        if (!await clientes.ExisteAsync(venda.ClienteId, cancelamento))
        {
            erros.Add(new Erro("ClienteId", "Cliente não encontrado."));
        }

        var itens = venda.Itens.ToList();
        var existentes = await produtos.FiltrarExistentesAsync(itens.Select(i => i.ProdutoId), cancelamento);
        for (var linha = 0; linha < itens.Count; linha++)
        {
            if (!existentes.Contains(itens[linha].ProdutoId))
            {
                erros.Add(new Erro($"Itens[{linha}].ProdutoId", "Produto não encontrado."));
            }
        }

        return erros;
    }
}
