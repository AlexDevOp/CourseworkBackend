using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using СourseworkBackend.Models;

namespace СourseworkBackend
{
    public partial class ProjectDatabase : DbContext
    {
        public ProjectDatabase()
        {
            
        }

        public ProjectDatabase(DbContextOptions<ProjectDatabase> options)
            : base(options)
        {
        }

        public virtual DbSet<CloudFile> CloudFiles { get; set; } = null!;
        public virtual DbSet<Session> Sessions { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<UserFileSystemStructure> UsersFileSystemStructures { get; set; } = null!;
        public virtual DbSet<UserTrustedDevice> UsersTrustedDevices { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("server=localhost;database=cloud_project;user=cloud_server;password=TestPassword", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.29-mysql"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_bin")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<CloudFile>(entity =>
            {
                entity.HasKey(e => e.Fileid)
                    .HasName("PRIMARY");

                entity.ToTable("cloud_files");

                entity.HasIndex(e => e.Userid, "ownerid_idx");

                entity.Property(e => e.Fileid).HasColumnName("fileid");

                entity.Property(e => e.ServersideName)
                    .HasMaxLength(512)
                    .HasColumnName("serverside_name");

                entity.Property(e => e.ServersideToken)
                    .HasMaxLength(512)
                    .HasColumnName("download_token");

                entity.Property(e => e.Userid).HasColumnName("userid");

                entity.Property(e => e.Lenght).HasColumnName("lenght");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CloudFiles)
                    .HasForeignKey(d => d.Userid)
                    .HasConstraintName("userid_cf_fk");
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("sessions");

                entity.HasIndex(e => e.Token, "token_UNIQUE")
                    .IsUnique();

                entity.HasIndex(e => e.Userid, "userid_idx");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Ip)
                    .HasMaxLength(128)
                    .HasColumnName("ip");

                entity.Property(e => e.Token)
                    .HasMaxLength(512)
                    .HasColumnName("token");

                entity.Property(e => e.LastTokenUseTime)
                    .HasColumnType("timestamp")
                    .ValueGeneratedOnAddOrUpdate()
                    .HasColumnName("last_token_use_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Userid).HasColumnName("userid");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Sessions)
                    .HasForeignKey(d => d.Userid)
                    .HasConstraintName("userid_s_fk");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasIndex(e => e.Login, "login_UNIQUE")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.UserName)
                    .HasMaxLength(256)
                    .HasColumnName("user_name");

                entity.Property(e => e.Login)
                    .HasMaxLength(128)
                    .HasColumnName("login");

                entity.Property(e => e.PassFingerprint)
                    .HasMaxLength(512)
                    .HasColumnName("pass_fingerprint");
            });

            modelBuilder.Entity<UserFileSystemStructure>(entity =>
            {
                entity.ToTable("users_file_system_structure");

                entity.HasIndex(e => e.Userid, "userid_idx");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.SerializedStructure)
                    .HasColumnType("json")
                    .HasColumnName("serialized_structure");

                entity.Property(e => e.Timestamp)
                    .HasColumnType("timestamp")
                    .ValueGeneratedOnAddOrUpdate()
                    .HasColumnName("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Userid).HasColumnName("userid");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UsersFileSystemStructures)
                    .HasForeignKey(d => d.Userid)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("userid_fss_fk");
            });

            modelBuilder.Entity<UserTrustedDevice>(entity =>
            {
                entity.HasKey(e => e.Deviceid)
                    .HasName("PRIMARY");

                entity.ToTable("users_trusted_devices");

                entity.HasIndex(e => e.Userid, "userid_idx");

                entity.Property(e => e.Deviceid).HasColumnName("deviceid");

                entity.Property(e => e.DeviceFingerprint)
                    .HasMaxLength(512)
                    .HasColumnName("device_fingerprint");

                entity.Property(e => e.DeviceToken)
                    .HasMaxLength(512)
                    .HasColumnName("device_token");

                entity.Property(e => e.Userid).HasColumnName("userid");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UsersTrustedDevices)
                    .HasForeignKey(d => d.Userid)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("userid_td_fk");
            });

            OnModelCreatingPartial(modelBuilder);

        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
