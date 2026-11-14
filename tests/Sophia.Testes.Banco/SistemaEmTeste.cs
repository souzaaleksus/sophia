using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Sophia.Application;
using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Painel;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Infrastructure;
using Sophia.Infrastructure.Persistencia;

namespace Sophia.Testes.Banco;

public sealed class SistemaEmTeste : IDisposable
{
    private const string FusoHorario = "America/Sao_Paulo";
    private static readonly TimeSpan HorarioDeBrasilia = TimeSpan.FromHours(-3);

    private readonly ServiceProvider _provedor;
    private readonly List<IServiceScope> _escopos = [];
    private Func<Task>? _antesDoProximoSalvamento;

    public SistemaEmTeste(BancoDeTestes banco, DateOnly hoje)
    {
        var servicos = new ServiceCollection();
        servicos.AddAplicacao();
        servicos.AddInfraestrutura(banco.TextoDeConexao, FusoHorario);
        servicos.AddSingleton<TimeProvider>(Tempo);
        servicos.AddScoped<IUnidadeDeTrabalho>(provedor => new UnidadeComGancho(
            new UnidadeDeTrabalho(provedor.GetRequiredService<SophiaDbContext>()),
            this));

        _provedor = servicos.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        Hoje = hoje;
    }

    public FakeTimeProvider Tempo { get; } = new();

    public DateOnly Hoje
    {
        get => _provedor.GetRequiredService<IRelogio>().Hoje;
        set => Tempo.SetUtcNow(new DateTimeOffset(value.ToDateTime(new TimeOnly(12, 0)), HorarioDeBrasilia));
    }

    public IClienteServico Clientes => Resolver<IClienteServico>();

    public IProdutoServico Produtos => Resolver<IProdutoServico>();

    public IVendaServico Vendas => Resolver<IVendaServico>();

    public IPainelServico Painel => Resolver<IPainelServico>();

    public T Resolver<T>()
        where T : notnull
    {
        var escopo = _provedor.CreateScope();
        _escopos.Add(escopo);
        return escopo.ServiceProvider.GetRequiredService<T>();
    }

    public void AntesDoProximoSalvamento(Func<Task> outraGravacao) => _antesDoProximoSalvamento = outraGravacao;

    public void Dispose()
    {
        foreach (var escopo in _escopos)
        {
            escopo.Dispose();
        }

        _provedor.Dispose();
    }

    private Func<Task>? RetirarGancho()
    {
        var gancho = _antesDoProximoSalvamento;
        _antesDoProximoSalvamento = null;
        return gancho;
    }

    private sealed class UnidadeComGancho(IUnidadeDeTrabalho unidade, SistemaEmTeste sistema) : IUnidadeDeTrabalho
    {
        public async Task SalvarAsync(CancellationToken cancelamento = default)
        {
            if (sistema.RetirarGancho() is { } outraGravacao)
            {
                await outraGravacao();
            }

            await unidade.SalvarAsync(cancelamento);
        }
    }
}
