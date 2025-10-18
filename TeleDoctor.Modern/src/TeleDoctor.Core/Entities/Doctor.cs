using System.ComponentModel.DataAnnotations;
using TeleDoctor.Core.Enums;
using TeleDoctor.Core.ValueObjects;

namespace TeleDoctor.Core.Entities;

public class Doctor : BaseEntity
{
    [Required]
    public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
    
    [Required]
    public ContactInfo ContactInfo { get; set; } = new ContactInfo();
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LicenseNumber { get; set; } = string.Empty;
    
    [Required]
    public Specialization Specialization { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Qualifications { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Biography { get; set; }
    
    public int YearsOfExperience { get; set; }
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    [Required]
    public TimeSpan EndTime { get; set; }
    
    [Required]
    public decimal ConsultationFee { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    public bool IsEmailVerified { get; set; } = false;
    
    public Guid? EmailVerificationToken { get; set; }
    
    public DateTime? EmailVerifiedAt { get; set; }
    
    public string? ResetPasswordToken { get; set; }
    
    public DateTime? ResetPasswordTokenExpiry { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastLoginAt { get; set; }
    
    public double AverageRating { get; set; } = 0.0;
    
    public int TotalReviews { get; set; } = 0;
    
    // Norwegian healthcare specific
    public string? HelsenorgeId { get; set; }
    
    public string? FastlegeNumber { get; set; }
    
    public bool IsNorwegianLicensed { get; set; } = false;
    
    // Navigation properties
    public virtual Department Department { get; set; } = null!;
    
    public int DepartmentId { get; set; }
    
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    
    public virtual ICollection<DoctorReview> Reviews { get; set; } = new List<DoctorReview>();
    
    public virtual ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    
    // AI-related properties
    public string? AIAssistantPreferences { get; set; } // JSON storing AI preferences
    
    public string? ClinicalDecisionSupport { get; set; } // AI-generated insights
    
    public DateTime? LastAITraining { get; set; }
    
    public string? AISpecializations { get; set; } // AI model specializations
}
