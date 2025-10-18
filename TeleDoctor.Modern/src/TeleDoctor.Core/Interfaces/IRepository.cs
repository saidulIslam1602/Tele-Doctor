using System.Linq.Expressions;
using TeleDoctor.Core.Entities;

namespace TeleDoctor.Core.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task DeleteRangeAsync(IEnumerable<T> entities);
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
    Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>> predicate);
}

public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByEmailAsync(string email);
    Task<Patient?> GetByNationalIdAsync(string nationalId);
    Task<IEnumerable<Patient>> GetPatientsWithUpcomingAppointmentsAsync();
    Task<IEnumerable<Patient>> SearchPatientsAsync(string searchTerm);
}

public interface IDoctorRepository : IRepository<Doctor>
{
    Task<Doctor?> GetByEmailAsync(string email);
    Task<Doctor?> GetByLicenseNumberAsync(string licenseNumber);
    Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync(DateTime date, TimeSpan time);
    Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(int specializationId);
    Task<IEnumerable<Doctor>> SearchDoctorsAsync(string searchTerm);
    Task<IEnumerable<Doctor>> GetTopRatedDoctorsAsync(int count = 10);
}

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(int patientId);
    Task<IEnumerable<Appointment>> GetAppointmentsByDoctorIdAsync(int doctorId);
    Task<IEnumerable<Appointment>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync();
    Task<IEnumerable<Appointment>> GetAppointmentsByStatusAsync(int status);
    Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime dateTime);
}

public interface IPrescriptionRepository : IRepository<Prescription>
{
    Task<IEnumerable<Prescription>> GetPrescriptionsByPatientIdAsync(int patientId);
    Task<IEnumerable<Prescription>> GetPrescriptionsByDoctorIdAsync(int doctorId);
    Task<Prescription?> GetByPrescriptionNumberAsync(string prescriptionNumber);
    Task<IEnumerable<Prescription>> GetActivePrescriptionsAsync(int patientId);
}

public interface IMedicalRecordRepository : IRepository<MedicalRecord>
{
    Task<IEnumerable<MedicalRecord>> GetMedicalRecordsByPatientIdAsync(int patientId);
    Task<IEnumerable<MedicalRecord>> GetMedicalRecordsByDoctorIdAsync(int doctorId);
    Task<IEnumerable<MedicalRecord>> SearchMedicalRecordsAsync(string searchTerm);
}

public interface IUnitOfWork : IDisposable
{
    IPatientRepository Patients { get; }
    IDoctorRepository Doctors { get; }
    IAppointmentRepository Appointments { get; }
    IPrescriptionRepository Prescriptions { get; }
    IMedicalRecordRepository MedicalRecords { get; }
    IRepository<Department> Departments { get; }
    IRepository<ChatMessage> ChatMessages { get; }
    IRepository<DoctorReview> DoctorReviews { get; }
    IRepository<SystemNotification> SystemNotifications { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
