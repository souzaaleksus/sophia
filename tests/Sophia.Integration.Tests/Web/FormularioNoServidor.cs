using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sophia.Application;
using Sophia.Web.Apoio;

namespace Sophia.Integration.Tests.Web;

internal static class FormularioNoServidor
{
    private const string Campo = "campo";

    public static async Task<(ModelBindingResult Resultado, IReadOnlyList<string> Erros)> LigarAsync(string propriedade, string digitado)
    {
        var culturaAnterior = CultureInfo.CurrentCulture;
        var culturaDaInterfaceAnterior = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
        try
        {
            await using var servicos = new ServiceCollection().AddLogging().AddSingleton(new ParametrosDoSophia()).AdicionarMvc().AdicionarNumerosDoBrasil().BuildServiceProvider();
            var metadados = servicos.GetRequiredService<IModelMetadataProvider>().GetMetadataForProperty(typeof(Formulario), propriedade);
            var ligador = servicos.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext
            {
                Metadata = metadados,
                BindingInfo = new BindingInfo { BindingSource = BindingSource.Query },
            });

            var contexto = await ContextoAsync(servicos, metadados, digitado);
            await ligador.BindModelAsync(contexto);
            var erros = contexto.ModelState.TryGetValue(Campo, out var entrada)
                ? entrada.Errors.Select(erro => erro.ErrorMessage).ToList()
                : [];
            return (contexto.Result, erros);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaAnterior;
            CultureInfo.CurrentUICulture = culturaDaInterfaceAnterior;
        }
    }

    private static async Task<ModelBindingContext> ContextoAsync(IServiceProvider servicos, ModelMetadata metadados, string digitado)
    {
        var http = new DefaultHttpContext { RequestServices = servicos };
        http.Request.QueryString = QueryString.Create(Campo, digitado);
        var acao = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var fontes = new ValueProviderFactoryContext(acao);
        await new QueryStringValueProviderFactory().CreateValueProviderAsync(fontes);

        return DefaultModelBindingContext.CreateBindingContext(
            acao,
            new CompositeValueProvider(fontes.ValueProviders),
            metadados,
            bindingInfo: null,
            modelName: Campo);
    }

    public sealed class Formulario
    {
        [Display(Name = "Quantidade")]
        public int? Quantidade { get; set; }

        [Display(Name = "Quantidade")]
        public int QuantidadeExigida { get; set; }

        [Display(Name = "Valor unitário")]
        public decimal? ValorUnitario { get; set; }

        [Display(Name = "Valor unitário")]
        public decimal ValorUnitarioExigido { get; set; }

        [Display(Name = "Data da venda")]
        public DateOnly DataExigida { get; set; }

        public int? Codigo { get; set; }
    }
}
