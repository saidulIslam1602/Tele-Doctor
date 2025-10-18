using System.ComponentModel.DataAnnotations;

namespace TeleDoctor.Core.Entities;

public class DoctorReview : BaseEntity
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [StringLength(1000)]
    public string? Comment { get; set; }
    
    [StringLength(1000)]
    public string? CommentNorwegian { get; set; }
    
    public bool IsAnonymous { get; set; } = false;
    
    public bool IsVerified { get; set; } = false;
    
    // Navigation properties
    public virtual Patient Patient { get; set; } = null!;
    
    public int PatientId { get; set; }
    
    public virtual Doctor Doctor { get; set; } = null!;
    
    public int DoctorId { get; set; }
    
    public virtual Appointment? Appointment { get; set; }
    
    public int? AppointmentId { get; set; }
    
    // AI-related properties
    public string? AISentimentScore { get; set; } // AI sentiment analysis
    
    public string? AIKeywords { get; set; } // AI-extracted keywords
    
    public bool AIFlaggedForReview { get; set; } = false; // AI flagged inappropriate content
}

public class DoctorSchedule : BaseEntity
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    [Required]
    public TimeSpan EndTime { get; set; }
    
    public TimeSpan? BreakStartTime { get; set; }
    
    public TimeSpan? BreakEndTime { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    public int SlotDurationMinutes { get; set; } = 30;
    
    // Navigation properties
    public virtual Doctor Doctor { get; set; } = null!;
    
    public int DoctorId { get; set; }
}

public class AppointmentDocument : BaseEntity
{
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string FileType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(100)]
    public string UploadedBy { get; set; } = string.Empty; // Patient or Doctor
    
    // Navigation properties
    public virtual Appointment Appointment { get; set; } = null!;
    
    public int AppointmentId { get; set; }
    
    // AI-related properties
    public string? AIDocumentAnalysis { get; set; } // AI analysis of document content
    
    public string? AIExtractedText { get; set; } // AI-extracted text from images/PDFs
    
    public string? AIClassification { get; set; } // AI document classification
}

public class SystemNotification : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string TitleNorwegian { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string? MessageNorwegian { get; set; }
    
    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = string.Empty; // Info, Warning, Error, Success
    
    [Required]
    [StringLength(50)]
    public string TargetUserType { get; set; } = string.Empty; // Patient, Doctor, Admin, All
    
    public int? TargetUserId { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // AI-related properties
    public bool IsAIGenerated { get; set; } = false;
    
    public string? AITrigger { get; set; } // What AI condition triggered this notification
}
