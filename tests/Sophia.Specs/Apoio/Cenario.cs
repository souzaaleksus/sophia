using System.Globalization;
using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain;
using Sophia.Tests.Fakes;

namespace Sophia.Specs.Apoio;

public sealed class Cenario
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public Cenario()
    {
        Relogio = new RelogioFixo(new DateOnly(2026, 11, 13));
        Clientes = new ClienteServico(Banco.Clientes, Banco, Relogio);
        Produtos = new ProdutoServico(Banco.Produtos, Banco);
        Vendas = new VendaServico(Banco.Vendas, Banco.Clientes, Banco.Produtos, Banco, Relogio);
    }

    public BancoEmMemoria Banco { get; } = new();

    public RelogioFixo Relogio { get; }

    public ClienteServico Clientes { get; }

    public ProdutoServico Produtos { get; }

    public VendaServico Vendas { get; }

    public Resultado? UltimoResultado { get; set; }

    public int? UltimaVendaId { get; set; }

    public static DateOnly? Data(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : DateOnly.ParseExact(texto, "dd/MM/yyyy", PtBr);

    public static decimal? Valor(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : decimal.Parse(texto, NumberStyles.Number, PtBr);

    public static string? Texto(string? texto) => string.IsNullOrEmpty(texto) ? null : texto;

    public async Task<ClienteDto> ClienteChamadoAsync(string nome) =>
        (await Clientes.ListarAsync(nome)).Single(c => c.Nome == nome);

    public async Task<ProdutoDto> ProdutoChamadoAsync(string nome) =>
        (await Produtos.ListarAsync(nome)).Single(p => p.Nome == nome);
}
