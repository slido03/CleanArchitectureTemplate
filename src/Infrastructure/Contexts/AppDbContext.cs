using CleanArchitecture.Domain.Entities.Sgcd;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Contexts;

public partial class BlazorHeroContext
{
    //Sgcpj db contexts
    public DbSet<ExternalApplication> ExternalApplications { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<DocumentVersion> DocumentVersions { get; set; }
    public DbSet<DocumentMatching> DocumentMatchings { get; set; }

    //configure lazyloading for nested entities
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies();
    }

}