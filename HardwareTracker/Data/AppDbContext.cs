using System;
using HardwareTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HardwareTracker.Data;

/// <summary>
/// Entity Framework Core database context managing persistence, schema configuration, and audit constraints.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AssignmentHistory> AssignmentHistories => Set<AssignmentHistory>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enforce DateTime properties to be saved and restored with DateTimeKind.Utc under SQLite text storage.
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.SerialNumber)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(a => a.SerialNumber)
                .IsUnique();

            entity.Property(a => a.Model)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(a => a.Category)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            // Unidirectional relationship without navigation collection on the Employee principal.
            entity.HasOne(a => a.AssignedEmployee)
                .WithMany()
                .HasForeignKey(a => a.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(a => a.AssignmentHistories)
                .WithOne(h => h.Asset)
                .HasForeignKey(h => h.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new
                {
                    Id = 1,
                    AssignedEmployeeId = 1,
                    Category = "Laptop",
                    Model = "Dell Latitude 5540",
                    SerialNumber = "LT-2024-001",
                    Status = AssetStatus.Assigned
                },
                new
                {
                    Id = 2,
                    AssignedEmployeeId = 2,
                    Category = "Laptop",
                    Model = "MacBook Pro 16",
                    SerialNumber = "LT-2024-002",
                    Status = AssetStatus.Assigned
                },
                new
                {
                    Id = 3,
                    Category = "Monitor",
                    Model = "Dell UltraSharp U2723QE",
                    SerialNumber = "MN-2024-101",
                    Status = AssetStatus.Available
                },
                new
                {
                    Id = 4,
                    Category = "Monitor",
                    Model = "LG 27UK850",
                    SerialNumber = "MN-2024-102",
                    Status = AssetStatus.InService
                },
                new
                {
                    Id = 5,
                    Category = "Peripheral",
                    Model = "Logitech MX Keys",
                    SerialNumber = "KB-2023-550",
                    Status = AssetStatus.Disposed
                });
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.Department)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.HireDate)
                .IsRequired();

            // Filtered index speeds up active employee lookups across the application.
            entity.HasIndex(e => e.TerminationDate)
                .HasFilter("[TerminationDate] IS NULL");

            entity.HasData(
                new
                {
                    Id = 1,
                    Department = "IT",
                    Email = "anna.kowalska@company.com",
                    FirstName = "Anna",
                    HireDate = new DateTime(2023, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc),
                    LastName = "Kowalska"
                },
                new
                {
                    Id = 2,
                    Department = "DevOps",
                    Email = "marek.nowak@company.com",
                    FirstName = "Marek",
                    HireDate = new DateTime(2023, 3, 15, 8, 0, 0, 0, DateTimeKind.Utc),
                    LastName = "Nowak"
                },
                new
                {
                    Id = 3,
                    Department = "HR",
                    Email = "zofia.wisniewska@company.com",
                    FirstName = "Zofia",
                    HireDate = new DateTime(2022, 11, 1, 8, 0, 0, 0, DateTimeKind.Utc),
                    LastName = "Wisniewska"
                });
        });

        modelBuilder.Entity<AssignmentHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.EmployeeName)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(h => h.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            // Composite index optimizes chronological timeline retrieval per physical unit.
            entity.HasIndex(h => new { h.AssetId, h.AssignedDate });

            // Unidirectional relationship preserves audit trails even if the employee is purged.
            entity.HasOne(h => h.Employee)
                .WithMany()
                .HasForeignKey(h => h.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasData(
                new
                {
                    Id = 1,
                    AssetId = 1,
                    AssignedDate = new DateTime(2024, 1, 15, 9, 0, 0, 0, DateTimeKind.Utc),
                    EmployeeId = 1,
                    EmployeeName = "Anna Kowalska",
                    Status = AssetStatus.Assigned
                },
                new
                {
                    Id = 2,
                    AssetId = 2,
                    AssignedDate = new DateTime(2024, 2, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                    EmployeeId = 2,
                    EmployeeName = "Marek Nowak",
                    Status = AssetStatus.Assigned
                },
                new
                {
                    Id = 3,
                    AssetId = 3,
                    AssignedDate = new DateTime(2023, 6, 1, 8, 30, 0, 0, DateTimeKind.Utc),
                    EmployeeId = 3,
                    EmployeeName = "Zofia Wisniewska",
                    ReturnedDate = new DateTime(2023, 12, 20, 16, 0, 0, 0, DateTimeKind.Utc),
                    Status = AssetStatus.Assigned
                },
                new
                {
                    Id = 4,
                    AssetId = 4,
                    AssignedDate = new DateTime(2024, 3, 1, 11, 0, 0, 0, DateTimeKind.Utc),
                    EmployeeName = "Service",
                    Status = AssetStatus.InService
                },
                new
                {
                    Id = 5,
                    AssetId = 5,
                    AssignedDate = new DateTime(2024, 4, 10, 14, 0, 0, 0, DateTimeKind.Utc),
                    EmployeeName = "Disposed",
                    ReturnedDate = new DateTime(2024, 4, 10, 14, 0, 0, 0, DateTimeKind.Utc),
                    Status = AssetStatus.Disposed
                });
        });
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}