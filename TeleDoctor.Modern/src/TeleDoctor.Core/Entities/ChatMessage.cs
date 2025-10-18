using System.ComponentModel.DataAnnotations;

namespace TeleDoctor.Core.Entities;

public class ChatMessage : BaseEntity
{
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string? ContentNorwegian { get; set; } // Auto-translated content
    
    [Required]
    public DateTime SentAt { get; set; }
    
    public DateTime? ReadAt { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    [Required]
    [StringLength(20)]
    public string SenderType { get; set; } = string.Empty; // Patient, Doctor, AI, System
    
    [StringLength(100)]
    public string? MessageType { get; set; } = "Text"; // Text, Image, File, AI_Response, System_Alert
    
    public string? AttachmentPath { get; set; }
    
    public string? AttachmentType { get; set; }
    
    public bool IsEmergency { get; set; } = false;
    
    public bool IsSystemGenerated { get; set; } = false;
    
    // Navigation properties
    public virtual Patient? Patient { get; set; }
    
    public int? PatientId { get; set; }
    
    public virtual Doctor? Doctor { get; set; }
    
    public int? DoctorId { get; set; }
    
    public virtual Appointment? Appointment { get; set; }
    
    public int? AppointmentId { get; set; }
    
    // AI-related properties
    public bool IsAIGenerated { get; set; } = false;
    
    public string? AIModel { get; set; } // Which AI model generated this message
    
    public string? AIPrompt { get; set; } // The prompt used to generate AI response
    
    public double? AIConfidence { get; set; } // AI confidence in the response
    
    public string? AISentimentAnalysis { get; set; } // AI sentiment analysis of the message
    
    public string? AILanguageDetected { get; set; } // AI-detected language
    
    public string? AITranslation { get; set; } // AI translation if needed
    
    public string? AIKeywords { get; set; } // AI-extracted keywords
    
    public bool AIRequiresAttention { get; set; } = false; // AI flagged for doctor attention
    
    public string? AIUrgencyLevel { get; set; } // AI-assessed urgency level
}

public class ChatSession : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string SessionId { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartedAt { get; set; }
    
    public DateTime? EndedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [StringLength(50)]
    public string Status { get; set; } = "Active"; // Active, Ended, Paused
    
    // Navigation properties
    public virtual Patient Patient { get; set; } = null!;
    
    public int PatientId { get; set; }
    
    public virtual Doctor Doctor { get; set; } = null!;
    
    public int DoctorId { get; set; }
    
    public virtual Appointment? Appointment { get; set; }
    
    public int? AppointmentId { get; set; }
    
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    
    // AI-related properties
    public string? AISessionSummary { get; set; } // AI-generated session summary
    
    public string? AIKeyTopics { get; set; } // AI-identified key topics discussed
    
    public string? AIActionItems { get; set; } // AI-identified action items
    
    public string? AIFollowUpSuggestions { get; set; } // AI follow-up suggestions
}
