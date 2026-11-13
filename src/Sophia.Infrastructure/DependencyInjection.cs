using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Infrastructure.Persistencia;

namespace Sophia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestrutura(
        this IServiceCollection services,
        string textoDeConexao,
        string fusoHorario)
    {
        services.AddDbContext<SophiaDbContext>(opcoes => opcoes.UseSqlServer(
            textoDeConexao,
            sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
        services.AddScoped<IVendaRepositorio, VendaRepositorio>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRelogio>(provedor => new RelogioDoSistema(
            provedor.GetRequiredService<TimeProvider>(),
            TimeZoneInfo.FindSystemTimeZoneById(fusoHorario)));

        return services;
    }
}
