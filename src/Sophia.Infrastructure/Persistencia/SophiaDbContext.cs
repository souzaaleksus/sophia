using Microsoft.EntityFrameworkCore;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;

namespace Sophia.Infrastructure.Persistencia;

public sealed class SophiaDbContext(DbContextOptions<SophiaDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<Venda> Vendas => Set<Venda>();

    public DbSet<ItemVenda> VendaItens => Set<ItemVenda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SophiaDbContext).Assembly);
    }
}
