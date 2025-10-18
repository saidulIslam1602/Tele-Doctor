using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;
using TeleDoctor.Infrastructure.Data;

namespace TeleDoctor.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation for all entities
/// Provides common CRUD operations and query functionality
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly TeleDoctorDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(TeleDoctorDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Retrieves an entity by its ID
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Retrieves all entities (respects soft delete query filter)
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary>
    /// Finds entities matching the specified predicate
    /// </summary>
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Returns the first entity matching the predicate or null
    /// </summary>
    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Adds a new entity to the database
    /// </summary>
    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Adds multiple entities to the database
    /// </summary>
    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
        return entities;
    }

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes an entity (soft delete by default)
    /// </summary>
    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes multiple entities (soft delete by default)
    /// </summary>
    public virtual async Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Counts all entities
    /// </summary>
    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    /// <summary>
    /// Counts entities matching the predicate
    /// </summary>
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }

    /// <summary>
    /// Checks if any entity exists matching the predicate
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    /// <summary>
    /// Retrieves a page of entities
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
    {
        return await _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a page of entities matching the predicate
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>> predicate)
    {
        return await _dbSet
            .Where(predicate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}

/// <summary>
/// Specialized repository for Patient entity with custom queries
/// </summary>
public class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(TeleDoctorDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Finds a patient by email address
    /// </summary>
    public async Task<Patient?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(p => p.Appointments)
            .FirstOrDefaultAsync(p => p.ContactInfo.Email == email);
    }

    /// <summary>
    /// Finds a patient by national ID number
    /// </summary>
    public async Task<Patient?> GetByNationalIdAsync(string nationalId)
    {
        return await _dbSet
            .Include(p => p.Appointments)
            .Include(p => p.MedicalRecords)
            .FirstOrDefaultAsync(p => p.PersonalInfo.NationalId == nationalId);
    }

    /// <summary>
    /// Gets all patients with upcoming appointments
    /// </summary>
    public async Task<IEnumerable<Patient>> GetPatientsWithUpcomingAppointmentsAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(p => p.Appointments)
            .Where(p => p.Appointments.Any(a => a.ScheduledDateTime > now))
            .ToListAsync();
    }

    /// <summary>
    /// Searches patients by name or email
    /// </summary>
    public async Task<IEnumerable<Patient>> SearchPatientsAsync(string searchTerm)
    {
        var lowerSearch = searchTerm.ToLower();
        return await _dbSet
            .Where(p => 
                p.PersonalInfo.FirstName.ToLower().Contains(lowerSearch) ||
                p.PersonalInfo.LastName.ToLower().Contains(lowerSearch) ||
                p.ContactInfo.Email.ToLower().Contains(lowerSearch))
            .Take(50)
            .ToListAsync();
    }
}

/// <summary>
/// Specialized repository for Doctor entity with custom queries
/// </summary>
public class DoctorRepository : Repository<Doctor>, IDoctorRepository
{
    public DoctorRepository(TeleDoctorDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Finds a doctor by email address
    /// </summary>
    public async Task<Doctor?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(d => d.Department)
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.ContactInfo.Email == email);
    }

    /// <summary>
    /// Finds a doctor by license number
    /// </summary>
    public async Task<Doctor?> GetByLicenseNumberAsync(string licenseNumber)
    {
        return await _dbSet
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.LicenseNumber == licenseNumber);
    }

    /// <summary>
    /// Gets doctors available at a specific date and time
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync(DateTime date, TimeSpan time)
    {
        var dayOfWeek = date.DayOfWeek;
        
        return await _dbSet
            .Include(d => d.Department)
            .Include(d => d.Schedules)
            .Where(d => d.IsAvailable && d.IsActive)
            .Where(d => d.Schedules.Any(s => 
                s.DayOfWeek == dayOfWeek &&
                s.IsAvailable &&
                s.StartTime <= time &&
                s.EndTime >= time))
            .ToListAsync();
    }

    /// <summary>
    /// Gets doctors by specialization
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(int specializationId)
    {
        return await _dbSet
            .Include(d => d.Department)
            .Where(d => (int)d.Specialization == specializationId && d.IsActive)
            .OrderByDescending(d => d.AverageRating)
            .ToListAsync();
    }

    /// <summary>
    /// Searches doctors by name, specialization, or license
    /// </summary>
    public async Task<IEnumerable<Doctor>> SearchDoctorsAsync(string searchTerm)
    {
        var lowerSearch = searchTerm.ToLower();
        return await _dbSet
            .Include(d => d.Department)
            .Where(d => 
                d.PersonalInfo.FirstName.ToLower().Contains(lowerSearch) ||
                d.PersonalInfo.LastName.ToLower().Contains(lowerSearch) ||
                d.Specialization.ToString().ToLower().Contains(lowerSearch) ||
                d.LicenseNumber.Contains(searchTerm))
            .Take(50)
            .ToListAsync();
    }

    /// <summary>
    /// Gets top-rated doctors
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetTopRatedDoctorsAsync(int count = 10)
    {
        return await _dbSet
            .Include(d => d.Department)
            .Where(d => d.IsActive && d.TotalReviews > 0)
            .OrderByDescending(d => d.AverageRating)
            .ThenByDescending(d => d.TotalReviews)
            .Take(count)
            .ToListAsync();
    }
}

/// <summary>
/// Specialized repository for Appointment entity with custom queries
/// </summary>
public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(TeleDoctorDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets all appointments for a specific patient
    /// </summary>
    public async Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(int patientId)
    {
        return await _dbSet
            .Include(a => a.Doctor)
            .ThenInclude(d => d.Department)
            .Include(a => a.Prescription)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.ScheduledDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all appointments for a specific doctor
    /// </summary>
    public async Task<IEnumerable<Appointment>> GetAppointmentsByDoctorIdAsync(int doctorId)
    {
        return await _dbSet
            .Include(a => a.Patient)
            .Include(a => a.Prescription)
            .Where(a => a.DoctorId == doctorId)
            .OrderByDescending(a => a.ScheduledDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Gets appointments within a date range
    /// </summary>
    public async Task<IEnumerable<Appointment>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.ScheduledDateTime >= startDate && a.ScheduledDateTime <= endDate)
            .OrderBy(a => a.ScheduledDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all upcoming appointments
    /// </summary>
    public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.ScheduledDateTime > now)
            .OrderBy(a => a.ScheduledDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Gets appointments by status
    /// </summary>
    public async Task<IEnumerable<Appointment>> GetAppointmentsByStatusAsync(int status)
    {
        return await _dbSet
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => (int)a.Status == status)
            .OrderByDescending(a => a.ScheduledDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a doctor is available at a specific datetime
    /// </summary>
    public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime dateTime)
    {
        // Check if doctor has any conflicting appointments
        var hasConflict = await _dbSet
            .AnyAsync(a => 
                a.DoctorId == doctorId &&
                a.ScheduledDateTime.Date == dateTime.Date &&
                Math.Abs((a.ScheduledDateTime - dateTime).TotalMinutes) < 30 &&
                a.Status != Core.Enums.AppointmentStatus.Cancelled);

        return !hasConflict;
    }
}

/// <summary>
/// Specialized repository for Prescription entity
/// </summary>
public class PrescriptionRepository : Repository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(TeleDoctorDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets all prescriptions for a patient
    /// </summary>
    public async Task<IEnumerable<Prescription>> GetPrescriptionsByPatientIdAsync(int patientId)
    {
        return await _dbSet
            .Include(p => p.Doctor)
            .Include(p => p.Medications)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.IssuedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all prescriptions issued by a doctor
    /// </summary>
    public async Task<IEnumerable<Prescription>> GetPrescriptionsByDoctorIdAsync(int doctorId)
    {
        return await _dbSet
            .Include(p => p.Patient)
            .Include(p => p.Medications)
            .Where(p => p.DoctorId == doctorId)
            .OrderByDescending(p => p.IssuedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Finds a prescription by its number
    /// </summary>
    public async Task<Prescription?> GetByPrescriptionNumberAsync(string prescriptionNumber)
    {
        return await _dbSet
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Medications)
            .FirstOrDefaultAsync(p => p.PrescriptionNumber == prescriptionNumber);
    }

    /// <summary>
    /// Gets active prescriptions for a patient
    /// </summary>
    public async Task<IEnumerable<Prescription>> GetActivePrescriptionsAsync(int patientId)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(p => p.Medications)
            .Where(p => 
                p.PatientId == patientId &&
                p.Status == Core.Enums.PrescriptionStatus.Issued &&
                (p.ExpiryDate == null || p.ExpiryDate > now))
            .OrderByDescending(p => p.IssuedDate)
            .ToListAsync();
    }
}

/// <summary>
/// Specialized repository for MedicalRecord entity
/// </summary>
public class MedicalRecordRepository : Repository<MedicalRecord>, IMedicalRecordRepository
{
    public MedicalRecordRepository(TeleDoctorDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets all medical records for a patient
    /// </summary>
    public async Task<IEnumerable<MedicalRecord>> GetMedicalRecordsByPatientIdAsync(int patientId)
    {
        return await _dbSet
            .Include(m => m.Doctor)
            .Include(m => m.Attachments)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.RecordDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all medical records created by a doctor
    /// </summary>
    public async Task<IEnumerable<MedicalRecord>> GetMedicalRecordsByDoctorIdAsync(int doctorId)
    {
        return await _dbSet
            .Include(m => m.Patient)
            .Include(m => m.Attachments)
            .Where(m => m.DoctorId == doctorId)
            .OrderByDescending(m => m.RecordDate)
            .ToListAsync();
    }

    /// <summary>
    /// Searches medical records by keywords
    /// </summary>
    public async Task<IEnumerable<MedicalRecord>> SearchMedicalRecordsAsync(string searchTerm)
    {
        var lowerSearch = searchTerm.ToLower();
        return await _dbSet
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Where(m => 
                m.Title.ToLower().Contains(lowerSearch) ||
                m.Content.ToLower().Contains(lowerSearch) ||
                m.Diagnosis != null && m.Diagnosis.ToLower().Contains(lowerSearch) ||
                m.DiagnosisCode != null && m.DiagnosisCode.Contains(searchTerm))
            .OrderByDescending(m => m.RecordDate)
            .Take(100)
            .ToListAsync();
    }
}
