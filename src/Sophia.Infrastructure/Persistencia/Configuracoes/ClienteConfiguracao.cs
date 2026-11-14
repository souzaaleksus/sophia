using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sophia.Domain.Clientes;

namespace Sophia.Infrastructure.Persistencia.Configuracoes;

internal sealed class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> cliente)
    {
        cliente.ToTable("Clientes", tabela =>
        {
            tabela.HasCheckConstraint("CK_Clientes_Cpf_SomenteDigitos", "Cpf NOT LIKE '%[^0-9]%'");
            tabela.HasCheckConstraint(
                "CK_Clientes_Telefone_SomenteDigitos",
                "Telefone NOT LIKE '%[^0-9]%' AND LEN(Telefone) IN (10, 11)");
        });
        cliente.HasKey(c => c.Id);
        ConfigurarCpf(cliente);
        ConfigurarNome(cliente);
        ConfigurarContato(cliente);
    }

    private static void ConfigurarCpf(EntityTypeBuilder<Cliente> cliente)
    {
        cliente.Property(c => c.Cpf)
            .HasConversion(cpf => cpf.Numero, numero => Cpf.DoBanco(numero))
            .HasColumnType("char(11)")
            .IsRequired();
        cliente.HasIndex(c => c.Cpf).IsUnique().HasDatabaseName("UQ_Clientes_Cpf");
    }

    private static void ConfigurarNome(EntityTypeBuilder<Cliente> cliente)
    {
        cliente.Property(c => c.Nome)
            .HasMaxLength(Cliente.TamanhoMaximoNome)
            .IsRequired()
            .UseCollation(Agrupamento.SemAcentoNemCaixa);
        cliente.HasIndex(c => c.Nome).HasDatabaseName("IX_Clientes_Nome");
    }

    private static void ConfigurarContato(EntityTypeBuilder<Cliente> cliente)
    {
        cliente.Property(c => c.DataNascimento).HasColumnType("date");
        cliente.Property(c => c.Email).HasMaxLength(Cliente.TamanhoMaximoEmail);
        cliente.Property(c => c.Telefone).HasColumnType("varchar(11)");
    }
}
