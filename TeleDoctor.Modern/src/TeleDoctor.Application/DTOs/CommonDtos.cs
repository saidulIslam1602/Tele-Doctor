using TeleDoctor.Core.Enums;

namespace TeleDoctor.Application.DTOs;

/// <summary>
/// DTO for appointment data transfer
/// </summary>
public class AppointmentDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public AppointmentStatus Status { get; set; }
    public ConsultationType ConsultationType { get; set; }
    public string? ReasonForVisit { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Related data
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
}

/// <summary>
/// DTO for creating a new appointment
/// </summary>
public class CreateAppointmentDto
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public ConsultationType ConsultationType { get; set; }
    public string? ReasonForVisit { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing appointment
/// </summary>
public class UpdateAppointmentDto
{
    public int Id { get; set; }
    public DateTime? ScheduledDateTime { get; set; }
    public int? DurationMinutes { get; set; }
    public AppointmentStatus? Status { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for patient information
/// </summary>
public class PatientDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
}

/// <summary>
/// DTO for doctor information
/// </summary>
public class DoctorDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Specialization { get; set; }
    public string? LicenseNumber { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>
/// DTO for prescription information
/// </summary>
public class PrescriptionDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Instructions { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
}

/// <summary>
/// DTO for medical record information
/// </summary>
public class MedicalRecordDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? DoctorId { get; set; }
    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordDate { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
}
