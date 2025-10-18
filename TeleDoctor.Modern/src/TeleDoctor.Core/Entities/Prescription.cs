using System.ComponentModel.DataAnnotations;
using TeleDoctor.Core.Enums;

namespace TeleDoctor.Core.Entities;

public class Prescription : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string PrescriptionNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime IssuedDate { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    [Required]
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    
    [StringLength(1000)]
    public string? Instructions { get; set; }
    
    [StringLength(1000)]
    public string? InstructionsNorwegian { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    public bool IsDigitallyVerified { get; set; } = false;
    
    public string? DigitalSignature { get; set; }
    
    // Norwegian e-Prescription integration
    public string? EReseptId { get; set; }
    
    public string? PharmacyId { get; set; }
    
    public DateTime? DispensedAt { get; set; }
    
    // Navigation properties
    [Required]
    public virtual Patient Patient { get; set; } = null!;
    
    public int PatientId { get; set; }
    
    [Required]
    public virtual Doctor Doctor { get; set; } = null!;
    
    public int DoctorId { get; set; }
    
    public virtual Appointment? Appointment { get; set; }
    
    public int? AppointmentId { get; set; }
    
    public virtual ICollection<PrescriptionMedication> Medications { get; set; } = new List<PrescriptionMedication>();
    
    // AI-related properties
    public string? AIInteractionWarnings { get; set; } // AI-detected drug interactions
    
    public string? AIAllergyWarnings { get; set; } // AI-detected allergy risks
    
    public string? AIDosageRecommendations { get; set; } // AI dosage suggestions
    
    public string? AIAlternativeMedications { get; set; } // AI alternative suggestions
}

public class PrescriptionMedication : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string MedicationName { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string MedicationNameNorwegian { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? GenericName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Dosage { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Frequency { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string FrequencyNorwegian { get; set; } = string.Empty;
    
    public int DurationDays { get; set; }
    
    public int Quantity { get; set; }
    
    [StringLength(500)]
    public string? SpecialInstructions { get; set; }
    
    [StringLength(500)]
    public string? SpecialInstructionsNorwegian { get; set; }
    
    // Norwegian medication codes
    public string? ATCCode { get; set; } // Anatomical Therapeutic Chemical code
    
    public string? VNRNumber { get; set; } // Norwegian medication number
    
    public string? LEGEMIDDELVERKETId { get; set; }
    
    // Navigation properties
    public virtual Prescription Prescription { get; set; } = null!;
    
    public int PrescriptionId { get; set; }
    
    // AI-related properties
    public string? AIEffectivenessScore { get; set; } // AI prediction of medication effectiveness
    
    public string? AISideEffectRisk { get; set; } // AI-calculated side effect risks
}
