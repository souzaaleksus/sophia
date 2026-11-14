using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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
        AdicionarMvc(builder.Services);
    }

    private static void AdicionarMvc(IServiceCollection servicos)
    {
        servicos.AddLocalization(opcoes => opcoes.ResourcesPath = Textos.Pasta);
        servicos
            .AddControllersWithViews(opcoes => opcoes.UsarDatasIso().RecusarConsultasInvalidas().Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .AddDataAnnotationsLocalization(opcoes =>
                opcoes.DataAnnotationLocalizerProvider = (_, fabrica) => fabrica.Create(typeof(Textos)));
        servicos
            .AddOptions<MvcOptions>()
            .Configure<IStringLocalizerFactory>((opcoes, fabrica) =>
                MensagensDeValidacao.TraduzirParaPortugues(opcoes.ModelBindingMessageProvider, fabrica.Create(typeof(Textos))));
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
        app.UseStaticFiles(ArquivosEstaticos.Opcoes);
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
