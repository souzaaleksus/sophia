using Microsoft.Extensions.Options;
using Sophia.Application;

namespace Sophia.Web.Apoio;

public static class ConfiguracaoDosParametros
{
    public const string Secao = "Sophia";

    public static IServiceCollection AddParametrosDoSophia(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddOptions<ParametrosDoSophia>()
            .Bind(configuracao.GetSection(Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(provedor => provedor.GetRequiredService<IOptions<ParametrosDoSophia>>().Value);
        return services;
    }
}
