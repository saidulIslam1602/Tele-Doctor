using System.ComponentModel.DataAnnotations;
using TeleDoctor.Core.Enums;
using TeleDoctor.Core.ValueObjects;

namespace TeleDoctor.Core.Entities;

public class Patient : BaseEntity
{
    [Required]
    public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
    
    [Required]
    public ContactInfo ContactInfo { get; set; } = new ContactInfo();
    
    public MedicalInfo MedicalInfo { get; set; } = new MedicalInfo();
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public bool IsEmailVerified { get; set; } = false;
    
    public Guid? EmailVerificationToken { get; set; }
    
    public DateTime? EmailVerifiedAt { get; set; }
    
    public string? ResetPasswordToken { get; set; }
    
    public DateTime? ResetPasswordTokenExpiry { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    
    // AI-related properties
    public string? AIHealthProfile { get; set; } // JSON storing AI-generated health insights
    
    public DateTime? LastAIAnalysis { get; set; }
    
    public string? RiskFactors { get; set; } // AI-identified risk factors
}
