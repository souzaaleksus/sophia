using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Application.Recursos;
using Sophia.Domain;
using Sophia.Domain.Vendas;

namespace Sophia.Application.Vendas;

public sealed class VendaServico(
    IVendaRepositorio vendas,
    IConsultasDeVendas consultas,
    IClienteRepositorio clientes,
    IProdutoRepositorio produtos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IRelogio relogio,
    ParametrosDoSophia parametros) : IVendaServico
{
    private static readonly Ordenacao<ColunaDeVendas> OrdemPadrao = new(ColunaDeVendas.Data, Decrescente: true);

    public DateOnly DataSugerida() => relogio.Hoje;

    public async Task<Lista<VendaResumoDto>> ListarAsync(ConsultaDeVendas consulta, CancellationToken cancelamento = default)
    {
        var ordem = Ordenacao.Interpretar(consulta.Ordem, OrdemPadrao);
        var pagina = await Paginacao.RecortarAsync(
            consulta.Pagina,
            numero => consultas.ListarPaginaAsync(
                consulta.Inicio,
                consulta.Fim,
                ordem,
                numero,
                parametros.TamanhoDaPagina,
                cancelamento));

        return new Lista<VendaResumoDto>(pagina, ordem.Texto);
    }

    public Task<VendaDetalheDto?> ObterAsync(int id, CancellationToken cancelamento = default) =>
        consultas.ObterDetalheAsync(id, cancelamento);

    public async Task<Resultado<int>> RegistrarAsync(DadosVenda dados, CancellationToken cancelamento = default)
    {
        var registro = Venda.Registrar(dados, relogio.Hoje);
        if (!registro.Sucesso)
        {
            return Resultado.Falha<int>(registro.Erros);
        }

        var venda = registro.Valor;
        if (await RegistradaPelaMesmaChaveAsync(venda, cancelamento) is { } existente)
        {
            return ConferirReenvio(venda, existente);
        }

        var erros = await VerificarCadastrosAsync(venda, cancelamento);
        return erros.Count > 0
            ? Resultado.Falha<int>(erros)
            : await GravarAsync(venda, cancelamento);
    }

    private static Resultado<int> ConferirReenvio(Venda enviada, Venda registrada) =>
        registrada.TemOMesmoConteudoQue(enviada)
            ? Resultado.Ok(registrada.Id)
            : Resultado.Falha<int>(new Erro(nameof(DadosVenda.Chave), Mensagens.FormularioJaUsado(registrada.Id)));

    private async Task<Resultado<int>> GravarAsync(Venda venda, CancellationToken cancelamento)
    {
        vendas.Adicionar(venda);
        try
        {
            await unidadeDeTrabalho.SalvarAsync(cancelamento);
            return Resultado.Ok(venda.Id);
        }
        catch (ConflitoAoSalvarException conflito) when (conflito.Tipo == TipoDeConflito.ValorDuplicado)
        {
            var vencedora = await RegistradaPelaMesmaChaveAsync(venda, cancelamento);
            if (vencedora is null)
            {
                throw;
            }

            return ConferirReenvio(venda, vencedora);
        }
    }

    private async Task<Venda?> RegistradaPelaMesmaChaveAsync(Venda venda, CancellationToken cancelamento) =>
        venda.ChaveDeRegistro is { } chave ? await vendas.ObterPelaChaveAsync(chave, cancelamento) : null;

    private async Task<List<Erro>> VerificarCadastrosAsync(Venda venda, CancellationToken cancelamento)
    {
        var erros = new List<Erro>();
        if (!await clientes.ExisteAsync(venda.ClienteId, cancelamento))
        {
            erros.Add(new Erro(nameof(DadosVenda.ClienteId), Mensagens.ClienteNaoEncontrado));
        }

        var itens = venda.Itens.ToList();
        var existentes = await produtos.FiltrarExistentesAsync(itens.Select(i => i.ProdutoId), cancelamento);
        for (var linha = 0; linha < itens.Count; linha++)
        {
            if (!existentes.Contains(itens[linha].ProdutoId))
            {
                erros.Add(new Erro(nameof(DadosItemVenda.ProdutoId), Mensagens.ProdutoNaoEncontrado, linha));
            }
        }

        return erros;
    }
}
