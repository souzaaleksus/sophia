using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Painel;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Infrastructure.Persistencia;

namespace Sophia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestrutura(
        this IServiceCollection services,
        string textoDeConexao,
        string fusoHorario) =>
        services
            .AdicionarBanco(textoDeConexao)
            .AdicionarRepositoriosEConsultas()
            .AdicionarRelogio(fusoHorario);

    private static IServiceCollection AdicionarBanco(this IServiceCollection services, string textoDeConexao) =>
        services
            .AddDbContext<SophiaDbContext>(opcoes => opcoes.UseSqlServer(
                textoDeConexao,
                sql => sql.EnableRetryOnFailure()))
            .AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

    private static IServiceCollection AdicionarRepositoriosEConsultas(this IServiceCollection services) =>
        services
            .AddScoped<IClienteRepositorio, ClienteRepositorio>()
            .AddScoped<IProdutoRepositorio, ProdutoRepositorio>()
            .AddScoped<IVendaRepositorio, VendaRepositorio>()
            .AddScoped<IConsultasDeVendas, ConsultasDeVendas>()
            .AddScoped<IConsultasDoPainel, ConsultasDoPainel>();

    private static IServiceCollection AdicionarRelogio(this IServiceCollection services, string fusoHorario) =>
        services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IRelogio>(provedor => new RelogioDoSistema(
                provedor.GetRequiredService<TimeProvider>(),
                TimeZoneInfo.FindSystemTimeZoneById(fusoHorario)));
}
