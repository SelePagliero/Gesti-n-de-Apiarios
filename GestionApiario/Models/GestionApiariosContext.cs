using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Models;

public partial class GestionApiariosContext : DbContext
{
    public GestionApiariosContext()
    {
    }

    public GestionApiariosContext(DbContextOptions<GestionApiariosContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alimento> Alimentos { get; set; }

    public virtual DbSet<Apiario> Apiarios { get; set; }

    public virtual DbSet<Campaña> Campañas { get; set; }

    public virtual DbSet<Controle> Controles { get; set; }

    public virtual DbSet<Enfermedad> Enfermedads { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Productosporenfermedad> Productosporenfermedads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alimento>(entity =>
        {
            entity.HasKey(e => e.Codigo).HasName("PK__ALIMENTO__06370DAD793DF6FA");

            entity.ToTable("ALIMENTOS");

            entity.Property(e => e.FechaAlta).HasColumnType("datetime");
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioAlta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioBaja)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioModificacion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Apiario>(entity =>
        {
            entity.HasKey(e => e.Codigo).HasName("PK__APIARIO__06370DADE50DDD3D");

            entity.ToTable("APIARIO");

            entity.Property(e => e.Empresa)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaAlta).HasColumnType("datetime");
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Latitud).HasMaxLength(50);
            entity.Property(e => e.Longitud).HasMaxLength(50);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioAlta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioBaja)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioModificacion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Campaña>(entity =>
        {
            entity.HasKey(e => e.Codigo).HasName("PK__CAMPAÑA__06370DAD7B4D3DD6");

            entity.ToTable("CAMPAÑA");

            entity.Property(e => e.FechaAlta).HasColumnType("datetime");
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Responsable)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioAlta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioBaja)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioModificacion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Controle>(entity =>
        {
            entity.HasKey(e => e.Codigo).HasName("PK__CONTROLE__06370DADD3C8B4F3");

            entity.ToTable("CONTROLES");

            entity.Property(e => e.CantProducto).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CantidadAlimento).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.FechaAlta).HasColumnType("datetime");
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Obsevaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioAlta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioBaja)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioModificacion)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.CodAlimentoNavigation).WithMany(p => p.Controles)
                .HasForeignKey(d => d.CodAlimento)
                .HasConstraintName("FK__CONTROLES__CodAl__59063A47");

            entity.HasOne(d => d.CodApiarioNavigation).WithMany(p => p.Controles)
                .HasForeignKey(d => d.CodApiario)
                .HasConstraintName("FK__CONTROLES__CodAp__5812160E");

            entity.HasOne(d => d.CodCampañaNavigation).WithMany(p => p.Controles)
                .HasForeignKey(d => d.CodCampaña)
                .HasConstraintName("FK__CONTROLES__CodCa__571DF1D5");

            entity.HasOne(d => d.CodEnfermedadNavigation).WithMany(p => p.Controles)
                .HasForeignKey(d => d.CodEnfermedad)
                .HasConstraintName("FK__CONTROLES__CodEn__59FA5E80");

            entity.HasOne(d => d.CodProductosNavigation).WithMany(p => p.Controles)
                .HasForeignKey(d => d.CodProductos)
                .HasConstraintName("FK__CONTROLES__CodPr__5AEE82B9");
        });

        modelBuilder.Entity<Enfermedad>(entity =>
        {
            entity.HasKey(e => e.Codigo).HasName("PK__ENFERMED__06370DAD59FB3587");

            entity.ToTable("ENFERMEDAD");

            entity.Property(e => e.FechaAlta).HasColumnType("datetime");
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioAlta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioBaja)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioModificacion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Codigo).HasName("PK__PRODUCTO__06370DAD5655F7E5");

            entity.ToTable("PRODUCTOS");

            entity.Property(e => e.FechaAlta).HasColumnType("datetime");
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioAlta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioBaja)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioModificacion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Productosporenfermedad>(entity =>
        {
            entity.HasKey(e => new { e.CodEnfermedad, e.CodProducto }).HasName("PK__PRODUCTO__4306209F17F69558");

            entity.ToTable("PRODUCTOSPORENFERMEDAD");

            entity.Property(e => e.FechaAlta).HasColumnType("datetime");
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.UsuarioAlta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioBaja)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioModificacion)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.CodEnfermedadNavigation).WithMany(p => p.Productosporenfermedads)
                .HasForeignKey(d => d.CodEnfermedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PRODUCTOS__CodEn__534D60F1");

            entity.HasOne(d => d.CodProductoNavigation).WithMany(p => p.Productosporenfermedads)
                .HasForeignKey(d => d.CodProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PRODUCTOS__CodPr__5441852A");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
