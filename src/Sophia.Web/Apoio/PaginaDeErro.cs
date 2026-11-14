using Microsoft.AspNetCore.Diagnostics;

namespace Sophia.Web.Apoio;

public static class PaginaDeErro
{
    private static readonly object ChaveDoMetodoOriginal = new();

    public static IApplicationBuilder ExibirErroComoLeitura(this IApplicationBuilder app) =>
        app.Use(async (contexto, proximo) =>
        {
            if (!EhReexecucao(contexto))
            {
                await proximo(contexto);
                return;
            }

            var metodoOriginal = contexto.Request.Method;
            contexto.Items[ChaveDoMetodoOriginal] = metodoOriginal;
            contexto.Request.Method = HttpMethods.Get;
            try
            {
                await proximo(contexto);
            }
            finally
            {
                contexto.Request.Method = metodoOriginal;
                contexto.Items.Remove(ChaveDoMetodoOriginal);
            }
        });

    public static string MetodoOriginal(HttpContext contexto) =>
        contexto.Items.TryGetValue(ChaveDoMetodoOriginal, out var metodo) && metodo is string original
            ? original
            : contexto.Request.Method;

    private static bool EhReexecucao(HttpContext contexto) =>
        contexto.Features.Get<IStatusCodeReExecuteFeature>() is not null
        || contexto.Features.Get<IExceptionHandlerFeature>() is not null;
}
