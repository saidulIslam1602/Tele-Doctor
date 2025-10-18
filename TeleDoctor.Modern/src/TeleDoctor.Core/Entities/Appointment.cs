using System.ComponentModel.DataAnnotations;
using TeleDoctor.Core.Enums;

namespace TeleDoctor.Core.Entities;

public class Appointment : BaseEntity
{
    [Required]
    public DateTime ScheduledDateTime { get; set; }
    
    public DateTime? ActualStartTime { get; set; }
    
    public DateTime? ActualEndTime { get; set; }
    
    [Required]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    
    [Required]
    public ConsultationType ConsultationType { get; set; } = ConsultationType.VideoCall;
    
    [StringLength(1000)]
    public string? ChiefComplaint { get; set; }
    
    [StringLength(2000)]
    public string? Symptoms { get; set; }
    
    public string? VitalSigns { get; set; } // JSON format
    
    public string? Notes { get; set; }
    
    public string? CancellationReason { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    public string? CancelledBy { get; set; }
    
    // Video call related
    public string? MeetingRoomId { get; set; }
    
    public string? MeetingUrl { get; set; }
    
    public string? RecordingUrl { get; set; }
    
    // Norwegian healthcare integration
    public string? HelsenorgeReferenceId { get; set; }
    
    public bool IsUrgent { get; set; } = false;
    
    public string? UrgencyReason { get; set; }
    
    // Navigation properties
    [Required]
    public virtual Patient Patient { get; set; } = null!;
    
    public int PatientId { get; set; }
    
    [Required]
    public virtual Doctor Doctor { get; set; } = null!;
    
    public int DoctorId { get; set; }
    
    public virtual Prescription? Prescription { get; set; }
    
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    
    public virtual ICollection<AppointmentDocument> Documents { get; set; } = new List<AppointmentDocument>();
    
    // AI-related properties
    public string? AIPreConsultationAnalysis { get; set; } // AI analysis of symptoms before appointment
    
    public string? AIPostConsultationSummary { get; set; } // AI-generated consultation summary
    
    public string? AISuggestedDiagnosis { get; set; } // AI diagnostic suggestions
    
    public string? AIRecommendedTests { get; set; } // AI-recommended diagnostic tests
    
    public string? AIFollowUpPlan { get; set; } // AI-suggested follow-up plan
    
    public double? AIUrgencyScore { get; set; } // AI-calculated urgency score (0-1)
    
    public string? AITranscription { get; set; } // AI transcription of the consultation
}
