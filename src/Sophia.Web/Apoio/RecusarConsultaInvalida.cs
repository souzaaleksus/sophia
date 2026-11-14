using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sophia.Web.Apoio;

public sealed class RecusarConsultaInvalida : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method) && !context.ModelState.IsValid)
        {
            context.Result = new BadRequestResult();
            return Task.CompletedTask;
        }

        return next();
    }
}

public static class ConsultasInvalidas
{
    public static MvcOptions RecusarConsultasInvalidas(this MvcOptions opcoes)
    {
        opcoes.Filters.Add(new RecusarConsultaInvalida());
        return opcoes;
    }
}
