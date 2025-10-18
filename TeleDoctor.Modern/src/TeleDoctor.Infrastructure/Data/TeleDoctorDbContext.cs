using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TeleDoctor.Core.Entities;

namespace TeleDoctor.Infrastructure.Data;

/// <summary>
/// Main database context for TeleDoctor application
/// Implements Entity Framework Core DbContext with all domain entities
/// Includes configurations for relationships, indexes, and constraints
/// </summary>
public class TeleDoctorDbContext : IdentityDbContext<IdentityUser>
{
    public TeleDoctorDbContext(DbContextOptions<TeleDoctorDbContext> options)
        : base(options)
    {
    }

    // Define DbSets for all domain entities
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<DepartmentService> DepartmentServices { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<PrescriptionMedication> PrescriptionMedications { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }
    public DbSet<MedicalRecordAttachment> MedicalRecordAttachments { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<DoctorReview> DoctorReviews { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    public DbSet<AppointmentDocument> AppointmentDocuments { get; set; }
    public DbSet<SystemNotification> SystemNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Patient entity
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);
            entity.OwnsOne(e => e.PersonalInfo);
            entity.OwnsOne(e => e.ContactInfo);
            entity.OwnsOne(e => e.MedicalInfo);
            
            // Configure relationships
            entity.HasMany(e => e.Appointments)
                  .WithOne(a => a.Patient)
                  .HasForeignKey(a => a.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasMany(e => e.Prescriptions)
                  .WithOne(p => p.Patient)
                  .HasForeignKey(p => p.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Doctor entity
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseNumber).IsUnique();
            entity.HasIndex(e => e.Specialization);
            entity.HasIndex(e => e.IsActive);
            entity.OwnsOne(e => e.PersonalInfo);
            entity.OwnsOne(e => e.ContactInfo);
            
            // Configure relationships
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Doctors)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasMany(e => e.Appointments)
                  .WithOne(a => a.Doctor)
                  .HasForeignKey(a => a.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Appointment entity
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PatientId, e.ScheduledDateTime });
            entity.HasIndex(e => new { e.DoctorId, e.ScheduledDateTime });
            entity.HasIndex(e => e.Status);
            
            // Configure relationships
            entity.HasOne(e => e.Prescription)
                  .WithOne(p => p.Appointment)
                  .HasForeignKey<Prescription>(p => p.AppointmentId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasMany(e => e.MedicalRecords)
                  .WithOne(m => m.Appointment)
                  .HasForeignKey(m => m.AppointmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Department entity
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            
            entity.HasMany(e => e.Services)
                  .WithOne(s => s.Department)
                  .HasForeignKey(s => s.DepartmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Prescription entity
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PrescriptionNumber).IsUnique();
            entity.HasIndex(e => new { e.PatientId, e.IssuedDate });
            
            entity.HasMany(e => e.Medications)
                  .WithOne(m => m.Prescription)
                  .HasForeignKey(m => m.PrescriptionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure MedicalRecord entity
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PatientId, e.RecordDate });
            entity.HasIndex(e => e.DiagnosisCode);
            
            entity.HasMany(e => e.Attachments)
                  .WithOne(a => a.MedicalRecord)
                  .HasForeignKey(a => a.MedicalRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ChatMessage entity
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PatientId, e.SentAt });
            entity.HasIndex(e => new { e.DoctorId, e.SentAt });
            entity.HasIndex(e => e.IsRead);
        });

        // Configure ChatSession entity
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => new { e.PatientId, e.DoctorId });
            
            entity.HasMany(e => e.Messages)
                  .WithOne()
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DoctorReview entity
        modelBuilder.Entity<DoctorReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DoctorId, e.CreatedAt });
        });

        // Configure DoctorSchedule entity
        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DoctorId, e.DayOfWeek });
        });

        // Global query filters for soft delete
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Doctor>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Prescription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalRecord>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Override SaveChanges to automatically set audit fields
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically set audit fields
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically updates audit fields (CreatedAt, UpdatedAt) for tracked entities
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    // Implement soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
