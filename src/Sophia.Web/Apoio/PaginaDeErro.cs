using Microsoft.AspNetCore.Diagnostics;

namespace Sophia.Web.Apoio;

public static class PaginaDeErro
{
    public static IApplicationBuilder ExibirErroComoLeitura(this IApplicationBuilder app) =>
        app.Use(async (contexto, proximo) =>
        {
            if (!EhReexecucao(contexto))
            {
                await proximo(contexto);
                return;
            }

            var metodoOriginal = contexto.Request.Method;
            contexto.Request.Method = HttpMethods.Get;
            try
            {
                await proximo(contexto);
            }
            finally
            {
                contexto.Request.Method = metodoOriginal;
            }
        });

    private static bool EhReexecucao(HttpContext contexto) =>
        contexto.Features.Get<IStatusCodeReExecuteFeature>() is not null
        || contexto.Features.Get<IExceptionHandlerFeature>() is not null;
}
