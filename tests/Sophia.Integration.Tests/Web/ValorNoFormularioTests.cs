using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sophia.Web.Apoio;

namespace Sophia.Integration.Tests.Web;

public sealed class ValorNoFormularioTests
{
    [Theory]
    [InlineData("1.250,00", "1250")]
    [InlineData("1.250.000,5", "1250000.5")]
    [InlineData("18,90", "18.90")]
    [InlineData("0,75", "0.75")]
    [InlineData(" 2,50 ", "2.50")]
    [InlineData("-2,50", "-2.50")]
    [InlineData("1.250", "1250")]
    [InlineData("1.250.000", "1250000")]
    [InlineData("1250", "1250")]
    [InlineData("12.50", "12.50")]
    [InlineData("12.5", "12.5")]
    [InlineData("0.75", "0.75")]
    public async Task Com_virgula_o_ponto_e_milhar_e_sem_virgula_um_ponto_com_ate_dois_digitos_e_decimal(string digitado, string esperado)
    {
        var (resultado, _) = await LigarAsync(digitado);

        resultado.IsModelSet.Should().BeTrue();
        resultado.Model.Should().Be(decimal.Parse(esperado, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData("12.5055")]
    [InlineData("1.25,0")]
    [InlineData("12,50.00")]
    [InlineData("1,2,3")]
    [InlineData("12.")]
    [InlineData("abc")]
    [InlineData("0.500")]
    [InlineData("0.050")]
    [InlineData("00.500")]
    [InlineData("0.500,00")]
    [InlineData("01.250")]
    public async Task Qualquer_outra_forma_e_recusada_com_mensagem_no_campo(string digitado)
    {
        var (resultado, erros) = await LigarAsync(digitado);

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().ContainSingle();
    }

    [Fact]
    public async Task Campo_vazio_fica_sem_valor_e_sem_erro()
    {
        var (resultado, erros) = await LigarAsync(string.Empty);

        resultado.Model.Should().BeNull();
        erros.Should().BeEmpty();
    }

    private static async Task<(ModelBindingResult Resultado, ModelErrorCollection Erros)> LigarAsync(string digitado)
    {
        var culturaAnterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        try
        {
            using var servicos = new ServiceCollection().AddLogging().AddControllersWithViews(opcoes => opcoes.UsarDecimaisPtBr()).Services.BuildServiceProvider();
            var metadados = servicos.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(typeof(decimal?));
            var ligador = servicos.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext
            {
                Metadata = metadados,
                BindingInfo = new BindingInfo { BindingSource = BindingSource.Query },
            });

            var contexto = await ContextoAsync(servicos, metadados, digitado);
            await ligador.BindModelAsync(contexto);
            var erros = contexto.ModelState.TryGetValue("valor", out var entrada) ? entrada.Errors : new ModelErrorCollection();
            return (contexto.Result, erros);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaAnterior;
        }
    }

    private static async Task<ModelBindingContext> ContextoAsync(IServiceProvider servicos, ModelMetadata metadados, string digitado)
    {
        var http = new DefaultHttpContext { RequestServices = servicos };
        http.Request.QueryString = QueryString.Create("valor", digitado);
        var acao = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var fontes = new ValueProviderFactoryContext(acao);
        await new QueryStringValueProviderFactory().CreateValueProviderAsync(fontes);

        return DefaultModelBindingContext.CreateBindingContext(
            acao,
            new CompositeValueProvider(fontes.ValueProviders),
            metadados,
            bindingInfo: null,
            modelName: "valor");
    }
}
