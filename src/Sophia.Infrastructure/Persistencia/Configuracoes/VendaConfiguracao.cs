using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia.Configuracoes;

internal sealed class VendaConfiguracao : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> venda)
    {
        venda.ToTable("Vendas");
        venda.HasKey(v => v.Id);
        venda.Property(v => v.Data).HasColumnType("date");
        venda.Ignore(v => v.Total);

        venda.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Vendas_Clientes");

        venda.HasMany(v => v.Itens)
            .WithOne()
            .HasForeignKey(i => i.VendaId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_VendaItens_Vendas");
        venda.Navigation(v => v.Itens).HasField("_itens").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ItemVendaConfiguracao : IEntityTypeConfiguration<ItemVenda>
{
    public void Configure(EntityTypeBuilder<ItemVenda> item)
    {
        item.ToTable("VendaItens");
        item.HasKey(i => i.Id);
        item.Property(i => i.ValorUnitario).HasPrecision(18, 2);
        item.Ignore(i => i.Subtotal);
        item.HasIndex(i => new { i.VendaId, i.ProdutoId }).IsUnique().HasDatabaseName("UQ_VendaItens_Venda_Produto");

        item.HasOne<Produto>()
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_VendaItens_Produtos");
    }
}
