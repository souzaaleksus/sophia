using System.ComponentModel.DataAnnotations;
using Sophia.Domain.Recursos;

namespace Sophia.Web.Apoio;

[AttributeUsage(AttributeTargets.Property)]
public sealed class TamanhoDoNomeAttribute(int tamanhoMaximo) : StringLengthAttribute(tamanhoMaximo)
{
    public override string FormatErrorMessage(string name) => Mensagens.NomeAcimaDoTamanho(MaximumLength);
}
