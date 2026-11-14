using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sophia.Application;
using Sophia.Web.Apoio;

namespace Sophia.Integration.Tests.Web;

public sealed class ParametrosNaSubidaTests
{
    [Fact]
    public async Task Os_valores_da_secao_sophia_chegam_aos_servicos()
    {
        using var aplicacao = Montar(new()
        {
            ["Sophia:TamanhoDaPagina"] = "10",
            ["Sophia:SugestoesNaBusca"] = "3",
            ["Sophia:QuantidadeMaximaPorItem"] = "500",
        });

        await aplicacao.StartAsync();

        aplicacao.Services.GetRequiredService<ParametrosDoSophia>().Should().BeEquivalentTo(new ParametrosDoSophia
        {
            TamanhoDaPagina = 10,
            SugestoesNaBusca = 3,
            QuantidadeMaximaPorItem = 500,
        });
        await aplicacao.StopAsync();
    }

    [Fact]
    public async Task Sem_a_secao_valem_os_valores_padrao()
    {
        using var aplicacao = Montar([]);

        await aplicacao.StartAsync();

        aplicacao.Services.GetRequiredService<ParametrosDoSophia>().Should().BeEquivalentTo(new ParametrosDoSophia());
        await aplicacao.StopAsync();
    }

    [Theory]
    [InlineData("TamanhoDaPagina", "0")]
    [InlineData("DiasNoGrafico", "40")]
    [InlineData("MaisVendidosNoPainel", "0")]
    [InlineData("UltimasVendasNoPainel", "-5")]
    [InlineData("SugestoesNaBusca", "500")]
    [InlineData("DiasDoAtalhoDePeriodo", "1")]
    [InlineData("QuantidadeMaximaPorItem", "0")]
    [InlineData("QuantidadeMaximaPorItem", "1000000")]
    public async Task Valor_fora_da_faixa_impede_a_subida(string parametro, string valor)
    {
        using var aplicacao = Montar(new() { [$"Sophia:{parametro}"] = valor });

        var subir = () => aplicacao.StartAsync();

        (await subir.Should().ThrowAsync<OptionsValidationException>())
            .Which.Message.Should().Contain(parametro);
    }

    private static IHost Montar(Dictionary<string, string?> valores)
    {
        var construtor = new HostApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        construtor.Configuration.AddInMemoryCollection(valores);
        construtor.Services.AddParametrosDoSophia(construtor.Configuration);
        return construtor.Build();
    }
}
