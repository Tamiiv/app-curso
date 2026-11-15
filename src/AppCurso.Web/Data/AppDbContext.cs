using AppCurso.Web.Domain;
using AppCurso.Web.Features.Account;

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppCurso.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole, string>(options), IDataProtectionKeyContext
{
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.ToTable("LessonProgress");
            entity.HasKey(progress => progress.Id);
            entity.Property(progress => progress.ModuleSlug).HasMaxLength(120).IsRequired();
            entity.Property(progress => progress.LessonSlug).HasMaxLength(160).IsRequired();
            entity.HasIndex(progress => new { progress.UserId, progress.ModuleSlug, progress.LessonSlug })
                .IsUnique();
            entity.HasIndex(progress => new { progress.UserId, progress.LastViewedAt });
            entity.HasOne(progress => progress.User)
                .WithMany()
                .HasForeignKey(progress => progress.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.ToTable("Bookmarks");
            entity.HasKey(bookmark => bookmark.Id);
            entity.Property(bookmark => bookmark.ModuleSlug).HasMaxLength(120).IsRequired();
            entity.Property(bookmark => bookmark.LessonSlug).HasMaxLength(160).IsRequired();
            entity.HasIndex(bookmark => new { bookmark.UserId, bookmark.ModuleSlug, bookmark.LessonSlug })
                .IsUnique();
            entity.HasOne(bookmark => bookmark.User)
                .WithMany()
                .HasForeignKey(bookmark => bookmark.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DataProtectionKey>(entity =>
        {
            entity.ToTable("DataProtectionKeys");
            entity.HasKey(key => key.Id);
        });
    }
}
