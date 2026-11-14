using Sophia.Application.Abstracoes;
using Sophia.Application.Recursos;
using Sophia.Domain;
using Sophia.Domain.Produtos;

namespace Sophia.Application.Produtos;

public sealed class ProdutoServico(
    IProdutoRepositorio produtos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ParametrosDoSophia parametros) : IProdutoServico
{
    private static readonly Ordenacao<ColunaDeProdutos> OrdemPadrao = new(ColunaDeProdutos.Nome, Decrescente: false);
    private static readonly Erro NomeDuplicado = new("Nome", Mensagens.NomeDeProdutoDuplicado);
    private static readonly Erro NaoEncontrado = new(string.Empty, Mensagens.ProdutoNaoEncontrado);
    private static readonly Erro JaVendido = new(string.Empty, Mensagens.ProdutoJaVendido);

    public async Task<Lista<ProdutoDto>> ListarAsync(ConsultaDeLista consulta, CancellationToken cancelamento = default)
    {
        var termo = Aparar(consulta.Busca);
        var ordem = Ordenacao.Interpretar(consulta.Ordem, OrdemPadrao);
        var pagina = await Paginacao.RecortarAsync(
            consulta.Pagina,
            numero => produtos.ListarPaginaAsync(termo, ordem, numero, parametros.TamanhoDaPagina, cancelamento));

        return new Lista<ProdutoDto>(pagina.Converter(ProdutoDto.De), ordem.Texto);
    }

    public async Task<IReadOnlyList<ProdutoDto>> BuscarAsync(
        string? trechoDoNome,
        int limite,
        CancellationToken cancelamento = default)
    {
        var encontrados = await produtos.ListarAsync(Aparar(trechoDoNome), limite, cancelamento);
        return encontrados.Select(ProdutoDto.De).ToList();
    }

    public async Task<ProdutoDto?> ObterAsync(int id, CancellationToken cancelamento = default)
    {
        var produto = await produtos.ObterAsync(id, cancelamento);
        return produto is null ? null : ProdutoDto.De(produto);
    }

    public async Task<Resultado<int>> CadastrarAsync(DadosProduto dados, CancellationToken cancelamento = default)
    {
        var criacao = Produto.Criar(dados);
        if (!criacao.Sucesso)
        {
            return Resultado.Falha<int>(criacao.Erros);
        }

        var produto = criacao.Valor;
        if (await produtos.ExisteComNomeAsync(produto.Nome, excetoId: null, cancelamento))
        {
            return Resultado.Falha<int>(NomeDuplicado);
        }

        produtos.Adicionar(produto);
        var gravacao = await SalvarAsync(cancelamento);
        return gravacao.Sucesso ? Resultado.Ok(produto.Id) : Resultado.Falha<int>(gravacao.Erros);
    }

    public async Task<Resultado> AlterarAsync(int id, DadosProduto dados, CancellationToken cancelamento = default)
    {
        var produto = await produtos.ObterAsync(id, cancelamento);
        if (produto is null)
        {
            return Resultado.Falha(NaoEncontrado);
        }

        var alteracao = produto.Alterar(dados);
        if (!alteracao.Sucesso)
        {
            return alteracao;
        }

        if (await produtos.ExisteComNomeAsync(produto.Nome, excetoId: id, cancelamento))
        {
            return Resultado.Falha(NomeDuplicado);
        }

        return await SalvarAsync(cancelamento);
    }

    public async Task<Resultado> ExcluirAsync(int id, CancellationToken cancelamento = default)
    {
        var produto = await produtos.ObterAsync(id, cancelamento);
        if (produto is null)
        {
            return Resultado.Falha(NaoEncontrado);
        }

        if (await produtos.FoiVendidoAsync(id, cancelamento))
        {
            return Resultado.Falha(JaVendido);
        }

        produtos.Remover(produto);
        return await SalvarAsync(cancelamento);
    }

    private static string? Aparar(string? trechoDoNome) =>
        string.IsNullOrWhiteSpace(trechoDoNome) ? null : trechoDoNome.Trim();

    private async Task<Resultado> SalvarAsync(CancellationToken cancelamento)
    {
        try
        {
            await unidadeDeTrabalho.SalvarAsync(cancelamento);
            return Resultado.Ok();
        }
        catch (ConflitoAoSalvarException conflito)
        {
            return Resultado.Falha(conflito.Tipo == TipoDeConflito.ValorDuplicado ? NomeDuplicado : JaVendido);
        }
    }
}
