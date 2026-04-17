using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Vaulted.Models;

public partial class ApplicationContext : DbContext
{
    public ApplicationContext()
    {
    }

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MediaCategory> MediaCategories { get; set; }

    public virtual DbSet<MediaItem> MediaItems { get; set; }

    public virtual DbSet<MediaPhoto> MediaPhotos { get; set; }

    public virtual DbSet<MediaQuote> MediaQuotes { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MediaCat__3214EC07F4EC3B8F");

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC07BFE8C7B7");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AverageRating)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(3, 2)");
            entity.Property(e => e.CoverPhotoUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DateCreated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateUpdated).HasColumnType("datetime");
            entity.Property(e => e.Deleted).HasDefaultValue(false);
            entity.Property(e => e.MediaLink).IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Category).WithMany(p => p.MediaItems)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Media_Category");
        });

        modelBuilder.Entity<MediaPhoto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MediaPho__3214EC07D3A05D45");

            entity.ToTable("MediaPhoto");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.PhotoUrl)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.Media).WithMany(p => p.MediaPhotos)
                .HasForeignKey(d => d.MediaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MediaPhoto_Media");
        });

        modelBuilder.Entity<MediaQuote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MediaQuo__3214EC07EFE9AA3E");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.QuoteFrom)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.QuoteText).IsUnicode(false);

            entity.HasOne(d => d.Media).WithMany(p => p.MediaQuotes)
                .HasForeignKey(d => d.MediaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MediaQuotes_Media");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Comments__3214EC07EBAF0554");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Content).IsUnicode(false);
            entity.Property(e => e.DateCreated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Media).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.MediaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comments_Media");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
