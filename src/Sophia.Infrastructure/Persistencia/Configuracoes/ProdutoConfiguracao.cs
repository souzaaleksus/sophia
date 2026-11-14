using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sophia.Domain.Produtos;

namespace Sophia.Infrastructure.Persistencia.Configuracoes;

internal sealed class ProdutoConfiguracao : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> produto)
    {
        produto.ToTable("Produtos", tabela =>
            tabela.HasCheckConstraint("CK_Produtos_ValorUnitario_Positivo", "ValorUnitario > 0"));
        produto.HasKey(p => p.Id);

        produto.Property(p => p.Nome)
            .HasMaxLength(Produto.TamanhoMaximoNome)
            .IsRequired()
            .UseCollation(Agrupamento.SemAcentoNemCaixa);
        produto.HasIndex(p => p.Nome).IsUnique().HasDatabaseName("UQ_Produtos_Nome");

        produto.Property(p => p.ValorUnitario).HasPrecision(18, 2);
    }
}
