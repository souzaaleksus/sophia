namespace Sophia.Web.Apoio;

public static class CabecalhosDeSeguranca
{
    private const string PoliticaDeConteudo =
        "default-src 'self'; script-src 'self'; style-src 'self' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public static IApplicationBuilder UsarCabecalhosDeSeguranca(this IApplicationBuilder app) =>
        app.Use((contexto, proximo) =>
        {
            var cabecalhos = contexto.Response.Headers;
            cabecalhos.XContentTypeOptions = "nosniff";
            cabecalhos.XFrameOptions = "DENY";
            cabecalhos.ContentSecurityPolicy = PoliticaDeConteudo;
            cabecalhos["Referrer-Policy"] = "strict-origin-when-cross-origin";
            return proximo(contexto);
        });
}
