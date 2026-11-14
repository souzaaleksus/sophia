using Sophia.Application.Abstracoes;
using Sophia.Domain;

namespace Sophia.Application.Tests;

public sealed class GravacaoDeCadastroTests
{
    private static readonly Erro Duplicado = new("Nome", "Já existe.");
    private static readonly Erro EmUso = new(string.Empty, "Está em uso.");

    [Fact]
    public async Task Gravacao_sem_conflito_e_aceita()
    {
        var resultado = await new Unidade(null).SalvarCadastroAsync(Duplicado, EmUso);

        resultado.Sucesso.Should().BeTrue();
    }

    [Theory]
    [InlineData(TipoDeConflito.ValorDuplicado, "Já existe.")]
    [InlineData(TipoDeConflito.RegistroEmUso, "Está em uso.")]
    public async Task Conflito_do_banco_vira_o_erro_do_cadastro(TipoDeConflito tipo, string mensagem)
    {
        var resultado = await new Unidade(new ConflitoAoSalvarException(tipo)).SalvarCadastroAsync(Duplicado, EmUso);

        resultado.Erros.Should().ContainSingle().Which.Mensagem.Should().Be(mensagem);
    }

    private sealed class Unidade(ConflitoAoSalvarException? conflito) : IUnidadeDeTrabalho
    {
        public Task SalvarAsync(CancellationToken cancelamento = default) =>
            conflito is null ? Task.CompletedTask : Task.FromException(conflito);
    }
}
