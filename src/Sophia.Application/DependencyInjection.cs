using Microsoft.Extensions.DependencyInjection;
using Sophia.Application.Clientes;
using Sophia.Application.Painel;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;

namespace Sophia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IClienteServico, ClienteServico>();
        services.AddScoped<IProdutoServico, ProdutoServico>();
        services.AddScoped<IVendaServico, VendaServico>();
        services.AddScoped<IPainelServico, PainelServico>();
        return services;
    }
}
