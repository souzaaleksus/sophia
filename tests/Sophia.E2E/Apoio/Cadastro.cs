using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Infrastructure;
using Sophia.Infrastructure.Persistencia;
using Sophia.Testes.Banco;

namespace Sophia.E2E.Apoio;

public sealed class Cadastro(BancoDeTestes banco)
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateOnly Hoje => Relogio().Hoje;

    public async Task<int> ClienteAsync(string cpf, string nome)
    {
        await using var contexto = banco.NovoContexto();
        var servico = new ClienteServico(new ClienteRepositorio(contexto), new UnidadeDeTrabalho(contexto), Relogio());
        var resultado = await servico.CadastrarAsync(new DadosCliente(cpf, nome, null, null, null));
        resultado.Sucesso.Should().BeTrue($"o cliente {nome} precisa existir para o teste");
        return resultado.Valor;
    }

    public async Task<int> ProdutoAsync(string nome, decimal valorUnitario)
    {
        await using var contexto = banco.NovoContexto();
        var servico = new ProdutoServico(new ProdutoRepositorio(contexto), new UnidadeDeTrabalho(contexto));
        var resultado = await servico.CadastrarAsync(new DadosProduto(nome, valorUnitario));
        resultado.Sucesso.Should().BeTrue($"o produto {nome} precisa existir para o teste");
        return resultado.Valor;
    }

    public async Task<int> VendaAsync(DateOnly data, int clienteId, params DadosItemVenda[] itens)
    {
        await using var contexto = banco.NovoContexto();
        var servico = new VendaServico(
            new VendaRepositorio(contexto),
            new ClienteRepositorio(contexto),
            new ProdutoRepositorio(contexto),
            new UnidadeDeTrabalho(contexto),
            Relogio());
        var resultado = await servico.RegistrarAsync(new DadosVenda(data, clienteId, itens));
        resultado.Sucesso.Should().BeTrue("a venda precisa existir para o teste");
        return resultado.Valor;
    }

    public async Task<int> QuantidadeDeVendasAsync()
    {
        var linhas = await banco.ConsultarAsync("SELECT CAST(COUNT(*) AS nvarchar(10)) FROM dbo.Vendas");
        return int.Parse(linhas.Single());
    }

    public async Task<int> QuantidadeDeClientesAsync()
    {
        var linhas = await banco.ConsultarAsync("SELECT CAST(COUNT(*) AS nvarchar(10)) FROM dbo.Clientes");
        return int.Parse(linhas.Single());
    }

    public async Task<IReadOnlyList<int>> ClientesAsync(int quantidade)
    {
        var ids = new List<int>();
        for (var numero = 1; numero <= quantidade; numero++)
        {
            ids.Add(await ClienteAsync(CpfValido(numero), $"Cliente {numero:D2}"));
        }

        return ids;
    }

    public async Task<IReadOnlyList<int>> ProdutosAsync(int quantidade)
    {
        var ids = new List<int>();
        for (var numero = 1; numero <= quantidade; numero++)
        {
            ids.Add(await ProdutoAsync($"Produto {numero:D2}", numero + 0.90m));
        }

        return ids;
    }

    public async Task<IReadOnlyList<int>> VendasAsync(int quantidade)
    {
        var cliente = await ClienteAsync(CpfValido(900), "Cliente das vendas");
        var produto = await ProdutoAsync("Produto das vendas", 2.50m);
        var ids = new List<int>();
        for (var numero = 1; numero <= quantidade; numero++)
        {
            ids.Add(await VendaAsync(Hoje, cliente, new DadosItemVenda(produto, numero, 2.50m)));
        }

        return ids;
    }

    public static string CpfValido(int numero)
    {
        var digitos = (200_000_000 + (numero * 7_919)).ToString("D9").Select(c => c - '0').ToList();
        digitos.Add(DigitoVerificador(digitos));
        digitos.Add(DigitoVerificador(digitos));
        var texto = string.Concat(digitos);
        return $"{texto[..3]}.{texto[3..6]}.{texto[6..9]}-{texto[9..]}";
    }

    private static int DigitoVerificador(IReadOnlyList<int> digitos)
    {
        var soma = 0;
        for (var posicao = 0; posicao < digitos.Count; posicao++)
        {
            soma += digitos[posicao] * (digitos.Count + 1 - posicao);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static RelogioDoSistema Relogio() => new(TimeProvider.System, Fuso);
}
