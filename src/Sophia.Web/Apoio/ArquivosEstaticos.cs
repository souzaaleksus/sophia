namespace Sophia.Web.Apoio;

public static class ArquivosEstaticos
{
    public static StaticFileOptions Opcoes { get; } = new()
    {
        OnPrepareResponse = contexto =>
        {
            if (contexto.File.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                contexto.Context.Response.Headers.CacheControl = "no-cache";
            }
        },
    };
}
