using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Sophia.Integration.Tests.Web;

public sealed class DataNaConsultaTests
{
    [Theory]
    [InlineData("?de=2026-11-01", 2026, 11, 1)]
    [InlineData("?de=2026-02-28", 2026, 2, 28)]
    public async Task O_valor_do_campo_de_data_chega_como_data(string consulta, int ano, int mes, int dia)
    {
        var resultado = await LigarAsync(consulta);

        resultado.IsModelSet.Should().BeTrue();
        resultado.Model.Should().Be(new DateOnly(ano, mes, dia));
    }

    [Theory]
    [InlineData("?de=")]
    [InlineData("?ate=2026-11-01")]
    public async Task Campo_de_data_vazio_ou_ausente_fica_sem_data(string consulta)
    {
        var resultado = await LigarAsync(consulta);

        resultado.Model.Should().BeNull();
    }

    private static async Task<ModelBindingResult> LigarAsync(string consulta)
    {
        var culturaAnterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        try
        {
            using var servicos = new ServiceCollection().AddLogging().AddControllersWithViews().Services.BuildServiceProvider();
            var metadados = servicos.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(typeof(DateOnly?));
            var ligador = servicos.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext
            {
                Metadata = metadados,
                BindingInfo = new BindingInfo { BindingSource = BindingSource.Query },
            });

            var http = new DefaultHttpContext { RequestServices = servicos };
            http.Request.QueryString = new QueryString(consulta);
            var acao = new ActionContext(http, new RouteData(), new ActionDescriptor());
            var fontes = new ValueProviderFactoryContext(acao);
            await new QueryStringValueProviderFactory().CreateValueProviderAsync(fontes);

            var contexto = DefaultModelBindingContext.CreateBindingContext(
                acao,
                new CompositeValueProvider(fontes.ValueProviders),
                metadados,
                bindingInfo: null,
                modelName: "de");
            await ligador.BindModelAsync(contexto);
            return contexto.Result;
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaAnterior;
        }
    }
}
