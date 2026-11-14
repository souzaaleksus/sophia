using Microsoft.Extensions.DependencyInjection;
using Sophia.Application.Clientes;
using Sophia.Application.Painel;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;

namespace Sophia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IServicoDeCadastro<ClienteDto, DadosCliente>, ClienteServico>();
        services.AddScoped<IServicoDeCadastro<ProdutoDto, DadosProduto>, ProdutoServico>();
        services.AddScoped<IVendaServico, VendaServico>();
        services.AddScoped<IPainelServico, PainelServico>();
        return services;
    }
}
