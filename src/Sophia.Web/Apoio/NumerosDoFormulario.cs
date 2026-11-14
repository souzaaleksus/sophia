using Microsoft.AspNetCore.Mvc;

namespace Sophia.Web.Apoio;

public static class NumerosDoFormulario
{
    public static IServiceCollection AdicionarNumerosDoBrasil(this IServiceCollection servicos)
    {
        servicos.Configure<MvcOptions>(opcoes => opcoes.UsarDecimaisPtBr().UsarInteirosNoLimite());
        return servicos;
    }
}
