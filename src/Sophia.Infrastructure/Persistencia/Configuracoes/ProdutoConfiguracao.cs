using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sophia.Domain.Produtos;

namespace Sophia.Infrastructure.Persistencia.Configuracoes;

internal sealed class ProdutoConfiguracao : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> produto)
    {
        produto.ToTable("Produtos");
        produto.HasKey(p => p.Id);

        produto.Property(p => p.Nome).HasMaxLength(Produto.TamanhoMaximoNome).IsRequired();
        produto.HasIndex(p => p.Nome).IsUnique().HasDatabaseName("UQ_Produtos_Nome");

        produto.Property(p => p.ValorUnitario).HasPrecision(18, 2);
    }
}
