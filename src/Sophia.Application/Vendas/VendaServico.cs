using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Application.Recursos;
using Sophia.Domain;
using Sophia.Domain.Vendas;
using MensagensDoDominio = Sophia.Domain.Recursos.Mensagens;

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
        var nomes = await produtos.ObterNomesAsync(ProdutosInformados(dados), cancelamento);
        var registro = Venda.Registrar(dados, relogio.Hoje, parametros.QuantidadeMaximaPorItem, nomes);
        if (!registro.Sucesso)
        {
            return Resultado.Falha<int>(registro.Erros);
        }

        var venda = registro.Valor;
        if (await RegistradaPelaMesmaChaveAsync(venda, cancelamento) is { } existente)
        {
            return ConferirReenvio(venda, existente);
        }

        return await ClienteAusenteAsync(venda, cancelamento) is { } ausente
            ? Resultado.Falha<int>(ausente)
            : await GravarAsync(venda, cancelamento);
    }

    private static IEnumerable<int> ProdutosInformados(DadosVenda dados) =>
        dados.Itens.Select(item => item.ProdutoId).OfType<int>().Where(id => id > 0);

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
        catch (ConflitoAoSalvarException conflito) when (conflito.Tipo == TipoDeConflito.RestricaoViolada)
        {
            var ausentes = await VerificarCadastrosAsync(venda, cancelamento);
            if (ausentes.Count == 0)
            {
                throw;
            }

            return Resultado.Falha<int>(ausentes);
        }
    }

    private async Task<Venda?> RegistradaPelaMesmaChaveAsync(Venda venda, CancellationToken cancelamento) =>
        venda.ChaveDeRegistro is { } chave ? await vendas.ObterPelaChaveAsync(chave, cancelamento) : null;

    private async Task<Erro?> ClienteAusenteAsync(Venda venda, CancellationToken cancelamento) =>
        await clientes.ExisteAsync(venda.ClienteId, cancelamento)
            ? null
            : new Erro(nameof(DadosVenda.ClienteId), Mensagens.ClienteNaoEncontrado);

    private async Task<List<Erro>> VerificarCadastrosAsync(Venda venda, CancellationToken cancelamento)
    {
        var erros = new List<Erro>();
        if (await ClienteAusenteAsync(venda, cancelamento) is { } clienteAusente)
        {
            erros.Add(clienteAusente);
        }

        var itens = venda.Itens.ToList();
        var existentes = await produtos.ObterNomesAsync(itens.Select(i => i.ProdutoId), cancelamento);
        for (var linha = 0; linha < itens.Count; linha++)
        {
            if (!existentes.ContainsKey(itens[linha].ProdutoId))
            {
                erros.Add(new Erro(nameof(DadosItemVenda.ProdutoId), MensagensDoDominio.ProdutoNaoEncontrado, linha));
            }
        }

        return erros;
    }
}
