using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sophia.Application;
using Sophia.Infrastructure;

namespace Sophia.Web.Apoio;

public static class Composicao
{
    private const string FusoHorarioPadrao = "America/Sao_Paulo";

    public static void AdicionarSophia(this WebApplicationBuilder builder)
    {
        var configuracao = builder.Configuration;
        var textoDeConexao = configuracao.GetConnectionString("Sophia")
            ?? throw new InvalidOperationException("Configure a conexão ConnectionStrings:Sophia.");

        builder.Services.AddParametrosDoSophia(configuracao);
        builder.Services.AddAplicacao();
        builder.Services.AddInfraestrutura(textoDeConexao, configuracao["Sophia:FusoHorario"] ?? FusoHorarioPadrao);
        builder.Services.AdicionarMvc();
    }

    public static IServiceCollection AddParametrosDoSophia(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddOptions<ParametrosDoSophia>()
            .Bind(configuracao.GetSection("Sophia"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(provedor => provedor.GetRequiredService<IOptions<ParametrosDoSophia>>().Value);
        return services;
    }

    public static IServiceCollection AdicionarMvc(this IServiceCollection servicos)
    {
        servicos.AddControllersWithViews(opcoes =>
        {
            opcoes.UsarDatasIso().UsarNumerosDoBrasil().RecusarConsultasInvalidas().Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            MensagensDeValidacao.TraduzirParaPortugues(opcoes.ModelBindingMessageProvider);
        });
        return servicos;
    }

    public static void UsarSophia(this WebApplication app)
    {
        app.UsarCabecalhosDeSeguranca();
        UsarPortuguesDoBrasil(app);
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Erro");
        }

        app.UseStatusCodePagesWithReExecute("/Erro/{0}");
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = contexto =>
            {
                if (contexto.File.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                {
                    contexto.Context.Response.Headers.CacheControl = "no-cache";
                }
            },
        });
        app.UseRouting();
        app.MapControllerRoute(name: "default", pattern: "{controller=Painel}/{action=Index}/{id?}");
    }

    private static void UsarPortuguesDoBrasil(WebApplication app)
    {
        var portuguesDoBrasil = CultureInfo.GetCultureInfo("pt-BR");
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(portuguesDoBrasil),
            SupportedCultures = [portuguesDoBrasil],
            SupportedUICultures = [portuguesDoBrasil],
        });
    }
}
