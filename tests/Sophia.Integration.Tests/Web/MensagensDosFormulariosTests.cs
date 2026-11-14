using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sophia.Web.Apoio;
using Sophia.Web.Models;

namespace Sophia.Integration.Tests.Web;

public sealed class MensagensDosFormulariosTests
{
    [Theory]
    [InlineData(typeof(ClienteFormulario), nameof(ClienteFormulario.Cpf), "data-val-required", "Informe o CPF.")]
    [InlineData(typeof(ClienteFormulario), nameof(ClienteFormulario.Nome), "data-val-required", "Informe o nome.")]
    [InlineData(typeof(ClienteFormulario), nameof(ClienteFormulario.Nome), "data-val-length", "O nome deve ter no máximo 150 caracteres.")]
    [InlineData(typeof(ClienteFormulario), nameof(ClienteFormulario.Email), "data-val-email", "E-mail inválido.")]
    [InlineData(typeof(ClienteFormulario), nameof(ClienteFormulario.Email), "data-val-length", "E-mail inválido.")]
    [InlineData(typeof(ProdutoFormulario), nameof(ProdutoFormulario.Nome), "data-val-length", "O nome deve ter no máximo 150 caracteres.")]
    [InlineData(typeof(ProdutoFormulario), nameof(ProdutoFormulario.ValorUnitario), "data-val-required", "Informe o valor unitário.")]
    public async Task O_navegador_recebe_a_mensagem_do_dominio(Type formulario, string campo, string atributo, string mensagem)
    {
        await using var servicos = Servicos();
        var metadados = servicos.GetRequiredService<IModelMetadataProvider>().GetMetadataForProperty(formulario, campo);
        var atributos = new Dictionary<string, string>(StringComparer.Ordinal);
        var contexto = new ClientModelValidationContext(
            new ActionContext(new DefaultHttpContext { RequestServices = servicos }, new RouteData(), new ActionDescriptor()),
            metadados,
            servicos.GetRequiredService<IModelMetadataProvider>(),
            atributos);

        foreach (var validador in ValidadoresDoNavegador(servicos, metadados))
        {
            validador.AddValidation(contexto);
        }

        atributos.Should().ContainKey(atributo).WhoseValue.Should().Be(mensagem);
    }

    [Theory]
    [InlineData(typeof(ClienteFormulario), nameof(ClienteFormulario.Nome), 151, "O nome deve ter no máximo 150 caracteres.")]
    [InlineData(typeof(ProdutoFormulario), nameof(ProdutoFormulario.Nome), 151, "O nome deve ter no máximo 150 caracteres.")]
    public async Task O_servidor_recusa_com_a_mensagem_do_dominio(Type formulario, string campo, int tamanho, string mensagem)
    {
        await using var servicos = Servicos();
        var metadados = servicos.GetRequiredService<IModelMetadataProvider>().GetMetadataForProperty(formulario, campo);
        var validadores = metadados.ValidatorMetadata.OfType<ValidationAttribute>();

        var erros = validadores
            .Select(validador => validador.GetValidationResult(new string('a', tamanho), new ValidationContext(new object()) { DisplayName = campo }))
            .Where(resultado => resultado is not null)
            .Select(resultado => resultado!.ErrorMessage);

        erros.Should().Equal(mensagem);
    }

    private static ServiceProvider Servicos()
    {
        var servicos = new ServiceCollection().AddLogging();
        var diagnostico = new DiagnosticListener(nameof(MensagensDosFormulariosTests));
        servicos.AddSingleton(diagnostico).AddSingleton<DiagnosticSource>(diagnostico);
        return servicos.AdicionarMvc().BuildServiceProvider();
    }

    private static IEnumerable<IClientModelValidator> ValidadoresDoNavegador(IServiceProvider servicos, ModelMetadata metadados)
    {
        var provedores = servicos.GetRequiredService<IOptions<MvcViewOptions>>().Value.ClientModelValidatorProviders;
        var itens = metadados.ValidatorMetadata.Select(dado => new ClientValidatorItem(dado)).ToList();
        var contexto = new ClientValidatorProviderContext(metadados, itens);
        foreach (var provedor in provedores)
        {
            provedor.CreateValidators(contexto);
        }

        return itens.Select(item => item.Validator).OfType<IClientModelValidator>();
    }
}
