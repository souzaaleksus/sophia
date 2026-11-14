using System.Globalization;
using Sophia.Application;
using Sophia.Application.Clientes;
using Sophia.Application.Painel;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain;
using Sophia.Testes.Banco;

namespace Sophia.Specs.Apoio;

public sealed class Cenario : IDisposable
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly SistemaEmTeste _sistema = new(Ganchos.Banco, new DateOnly(2026, 11, 13));

    public DateOnly Hoje
    {
        get => _sistema.Hoje;
        set => _sistema.Hoje = value;
    }

    public IClienteServico Clientes => _sistema.Clientes;

    public IProdutoServico Produtos => _sistema.Produtos;

    public IVendaServico Vendas => _sistema.Vendas;

    public IPainelServico Painel => _sistema.Painel;

    public Resultado? UltimoResultado { get; set; }

    public int? UltimaVendaId { get; set; }

    public static DateOnly? Data(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : DateOnly.ParseExact(texto, "dd/MM/yyyy", PtBr);

    public static decimal? Valor(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : decimal.Parse(texto, NumberStyles.Number, PtBr);

    public static string? Texto(string? texto) => string.IsNullOrEmpty(texto) ? null : texto;

    public async Task<ClienteDto> ClienteChamadoAsync(string nome) =>
        (await Clientes.ListarAsync(new ConsultaDeLista(Busca: nome))).Pagina.Itens.Single(c => c.Nome == nome);

    public async Task<ProdutoDto> ProdutoChamadoAsync(string nome) =>
        (await Produtos.ListarAsync(new ConsultaDeLista(Busca: nome))).Pagina.Itens.Single(p => p.Nome == nome);

    public void Dispose() => _sistema.Dispose();
}
