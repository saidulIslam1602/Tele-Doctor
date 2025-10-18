using Microsoft.EntityFrameworkCore.Storage;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Interfaces;
using TeleDoctor.Infrastructure.Data;

namespace TeleDoctor.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions and repository access
/// Ensures all repository operations within a unit of work share the same database context
/// Provides transaction support for atomic operations across multiple repositories
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TeleDoctorDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy-loaded repositories
    private IPatientRepository? _patients;
    private IDoctorRepository? _doctors;
    private IAppointmentRepository? _appointments;
    private IPrescriptionRepository? _prescriptions;
    private IMedicalRecordRepository? _medicalRecords;
    private IRepository<Department>? _departments;
    private IRepository<ChatMessage>? _chatMessages;
    private IRepository<DoctorReview>? _doctorReviews;
    private IRepository<SystemNotification>? _systemNotifications;

    public UnitOfWork(TeleDoctorDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the Patient repository instance (lazy-loaded)
    /// </summary>
    public IPatientRepository Patients
    {
        get
        {
            _patients ??= new PatientRepository(_context);
            return _patients;
        }
    }

    /// <summary>
    /// Gets the Doctor repository instance (lazy-loaded)
    /// </summary>
    public IDoctorRepository Doctors
    {
        get
        {
            _doctors ??= new DoctorRepository(_context);
            return _doctors;
        }
    }

    /// <summary>
    /// Gets the Appointment repository instance (lazy-loaded)
    /// </summary>
    public IAppointmentRepository Appointments
    {
        get
        {
            _appointments ??= new AppointmentRepository(_context);
            return _appointments;
        }
    }

    /// <summary>
    /// Gets the Prescription repository instance (lazy-loaded)
    /// </summary>
    public IPrescriptionRepository Prescriptions
    {
        get
        {
            _prescriptions ??= new PrescriptionRepository(_context);
            return _prescriptions;
        }
    }

    /// <summary>
    /// Gets the MedicalRecord repository instance (lazy-loaded)
    /// </summary>
    public IMedicalRecordRepository MedicalRecords
    {
        get
        {
            _medicalRecords ??= new MedicalRecordRepository(_context);
            return _medicalRecords;
        }
    }

    /// <summary>
    /// Gets the Department repository instance (lazy-loaded)
    /// </summary>
    public IRepository<Department> Departments
    {
        get
        {
            _departments ??= new Repository<Department>(_context);
            return _departments;
        }
    }

    /// <summary>
    /// Gets the ChatMessage repository instance (lazy-loaded)
    /// </summary>
    public IRepository<ChatMessage> ChatMessages
    {
        get
        {
            _chatMessages ??= new Repository<ChatMessage>(_context);
            return _chatMessages;
        }
    }

    /// <summary>
    /// Gets the DoctorReview repository instance (lazy-loaded)
    /// </summary>
    public IRepository<DoctorReview> DoctorReviews
    {
        get
        {
            _doctorReviews ??= new Repository<DoctorReview>(_context);
            return _doctorReviews;
        }
    }

    /// <summary>
    /// Gets the SystemNotification repository instance (lazy-loaded)
    /// </summary>
    public IRepository<SystemNotification> SystemNotifications
    {
        get
        {
            _systemNotifications ??= new Repository<SystemNotification>(_context);
            return _systemNotifications;
        }
    }

    /// <summary>
    /// Saves all changes made in this unit of work to the database
    /// </summary>
    /// <returns>Number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Begins a new database transaction
    /// All subsequent operations will be part of this transaction until committed or rolled back
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Commits the current transaction
    /// Persists all changes made within the transaction to the database
    /// </summary>
    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction has been started. Call BeginTransactionAsync first.");
        }

        try
        {
            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction
    /// Discards all changes made within the transaction
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Disposes the unit of work and releases database connections
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
