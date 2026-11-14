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
        venda.HasIndex(v => v.Data).IsDescending().HasDatabaseName("IX_Vendas_Data");
        venda.Ignore(v => v.Total);
        ConfigurarChaveDeRegistro(venda);
        ConfigurarCliente(venda);
        ConfigurarItens(venda);
    }

    private static void ConfigurarChaveDeRegistro(EntityTypeBuilder<Venda> venda)
    {
        venda.Property(v => v.ChaveDeRegistro).HasColumnType("uniqueidentifier");
        venda.HasIndex(v => v.ChaveDeRegistro)
            .IsUnique()
            .HasFilter("[ChaveDeRegistro] IS NOT NULL")
            .HasDatabaseName("UQ_Vendas_ChaveDeRegistro");
    }

    private static void ConfigurarCliente(EntityTypeBuilder<Venda> venda) =>
        venda.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Vendas_Clientes");

    private static void ConfigurarItens(EntityTypeBuilder<Venda> venda)
    {
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
        ConfigurarTabela(item);
        item.HasKey(i => i.Id);
        item.Property(i => i.ValorUnitario).HasPrecision(18, 2);
        item.Ignore(i => i.Subtotal);
        item.HasIndex(i => new { i.VendaId, i.ProdutoId }).IsUnique().HasDatabaseName("UQ_VendaItens_Venda_Produto");
        ConfigurarProduto(item);
    }

    private static void ConfigurarTabela(EntityTypeBuilder<ItemVenda> item) =>
        item.ToTable("VendaItens", tabela =>
        {
            tabela.HasCheckConstraint("CK_VendaItens_Quantidade_Positiva", "Quantidade > 0");
            tabela.HasCheckConstraint("CK_VendaItens_QuantidadeMaxima", "Quantidade <= 999999");
            tabela.HasCheckConstraint("CK_VendaItens_ValorUnitario_Positivo", "ValorUnitario > 0");
        });

    private static void ConfigurarProduto(EntityTypeBuilder<ItemVenda> item) =>
        item.HasOne<Produto>()
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_VendaItens_Produtos");
}
