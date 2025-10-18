using System.ComponentModel.DataAnnotations;

namespace TeleDoctor.Core.Entities;

public class MedicalRecord : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string TitleNorwegian { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string? ContentNorwegian { get; set; }
    
    [Required]
    public DateTime RecordDate { get; set; }
    
    [StringLength(100)]
    public string RecordType { get; set; } = string.Empty; // Consultation, Lab Result, Imaging, etc.
    
    [StringLength(100)]
    public string? DiagnosisCode { get; set; } // ICD-10 code
    
    [StringLength(500)]
    public string? Diagnosis { get; set; }
    
    [StringLength(500)]
    public string? DiagnosisNorwegian { get; set; }
    
    public string? VitalSigns { get; set; } // JSON format
    
    public string? LabResults { get; set; } // JSON format
    
    public string? ImagingResults { get; set; } // JSON format
    
    public bool IsConfidential { get; set; } = false;
    
    // Norwegian healthcare integration
    public string? HelsenorgeRecordId { get; set; }
    
    public string? EPJSystemId { get; set; } // Electronic Patient Journal system ID
    
    // Navigation properties
    [Required]
    public virtual Patient Patient { get; set; } = null!;
    
    public int PatientId { get; set; }
    
    [Required]
    public virtual Doctor Doctor { get; set; } = null!;
    
    public int DoctorId { get; set; }
    
    public virtual Appointment? Appointment { get; set; }
    
    public int? AppointmentId { get; set; }
    
    public virtual ICollection<MedicalRecordAttachment> Attachments { get; set; } = new List<MedicalRecordAttachment>();
    
    // AI-related properties
    public string? AIGeneratedSummary { get; set; } // AI-generated summary of the record
    
    public string? AIExtractedSymptoms { get; set; } // AI-extracted symptoms from text
    
    public string? AIRiskAssessment { get; set; } // AI risk assessment
    
    public string? AIRecommendations { get; set; } // AI recommendations
    
    public string? AIKeywords { get; set; } // AI-extracted keywords for search
    
    public double? AISeverityScore { get; set; } // AI-calculated severity score (0-1)
}

public class MedicalRecordAttachment : BaseEntity
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
    
    public bool IsImage { get; set; } = false;
    
    // Navigation properties
    public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    
    public int MedicalRecordId { get; set; }
    
    // AI-related properties for medical images
    public string? AIImageAnalysis { get; set; } // AI analysis of medical images
    
    public string? AIDetectedAbnormalities { get; set; } // AI-detected abnormalities in images
    
    public double? AIConfidenceScore { get; set; } // AI confidence in image analysis
}
