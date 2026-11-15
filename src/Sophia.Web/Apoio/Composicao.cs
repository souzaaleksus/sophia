using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
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
        builder.Services.AdicionarMvc().AdicionarNumerosDoBrasil();
    }

    public static IServiceCollection AdicionarMvc(this IServiceCollection servicos)
    {
        servicos.AddControllersWithViews(opcoes =>
        {
            opcoes.UsarDatasIso().RecusarConsultasInvalidas().Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
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
        app.ExibirErroComoLeitura();
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
