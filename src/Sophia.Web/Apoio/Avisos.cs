using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Sophia.Web.Apoio;

public static class Avisos
{
    public const string ChaveDeSucesso = "Sucesso";
    public const string ChaveDeFalha = "Falha";

    public static void Sucesso(this ITempDataDictionary tempData, string mensagem) => tempData[ChaveDeSucesso] = mensagem;

    public static void Falha(this ITempDataDictionary tempData, string mensagem) => tempData[ChaveDeFalha] = mensagem;
}
