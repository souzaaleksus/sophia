using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Sophia.Application;
using Sophia.Infrastructure;
using Sophia.Web.Apoio;

var builder = WebApplication.CreateBuilder(args);

var textoDeConexao = builder.Configuration.GetConnectionString("Sophia")
    ?? throw new InvalidOperationException("Configure a conexão ConnectionStrings:Sophia.");
var fusoHorario = builder.Configuration["Sophia:FusoHorario"] ?? "America/Sao_Paulo";

builder.Services.AddAplicacao();
builder.Services.AddInfraestrutura(textoDeConexao, fusoHorario);
builder.Services.AddControllersWithViews(opcoes =>
{
    opcoes.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    MensagensDeValidacao.TraduzirParaPortugues(opcoes.ModelBindingMessageProvider);
});

var app = builder.Build();

var portuguesDoBrasil = CultureInfo.GetCultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(portuguesDoBrasil),
    SupportedCultures = [portuguesDoBrasil],
    SupportedUICultures = [portuguesDoBrasil],
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Erro");
}

app.UseStatusCodePagesWithReExecute("/Erro/{0}");
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Vendas}/{action=Index}/{id?}");

app.Run();
