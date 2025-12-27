using System.Text.Json.Serialization;

namespace TeleDoctor.AI.Services.Models;

public class ClinicalAnalysisRequest
{
    public string Symptoms { get; set; } = string.Empty;
    public string PatientHistory { get; set; } = string.Empty;
    public string? CurrentMedications { get; set; }
    public string? Allergies { get; set; }
    public int PatientAge { get; set; }
    public string? Gender { get; set; }
    public string? VitalSigns { get; set; }
    public string Language { get; set; } = "no";
}

public class ClinicalAnalysisResponse
{
    public List<DifferentialDiagnosis> DifferentialDiagnoses { get; set; } = new();
    public List<string> RecommendedTests { get; set; } = new();
    public List<string> RedFlags { get; set; } = new();
    public List<string> FollowUpRecommendations { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string SummaryNorwegian { get; set; } = string.Empty;
    public UrgencyAssessment Urgency { get; set; } = new();
}

public class DifferentialDiagnosis
{
    public string Diagnosis { get; set; } = string.Empty;
    public string DiagnosisNorwegian { get; set; } = string.Empty;
    public string ICD10Code { get; set; } = string.Empty;
    public double Probability { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public string ReasoningNorwegian { get; set; } = string.Empty;
    public List<string> SupportingSymptoms { get; set; } = new();
    public List<string> RequiredTests { get; set; } = new();
}

public class UrgencyAssessment
{
    public string Level { get; set; } = string.Empty; // Low, Medium, High, Critical
    public double Score { get; set; } // 0-1 scale
    public string Reasoning { get; set; } = string.Empty;
    public string ReasoningNorwegian { get; set; } = string.Empty;
    public bool RequiresImmediateAttention { get; set; }
    public string RecommendedTimeframe { get; set; } = string.Empty;
}

public class MedicationInteractionRequest
{
    public List<string> CurrentMedications { get; set; } = new();
    public string NewMedication { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    public string? Gender { get; set; }
    public string? Allergies { get; set; }
    public string? KidneyFunction { get; set; }
    public string? LiverFunction { get; set; }
}

public class MedicationInteractionResponse
{
    public List<DrugInteraction> Interactions { get; set; } = new();
    public List<AllergyWarning> AllergyWarnings { get; set; } = new();
    public DosageRecommendation DosageRecommendation { get; set; } = new();
    public List<string> AlternativeMedications { get; set; } = new();
    public List<string> MonitoringRecommendations { get; set; } = new();
    public string OverallRiskLevel { get; set; } = string.Empty;
}

public class DrugInteraction
{
    public string Drug1 { get; set; } = string.Empty;
    public string Drug2 { get; set; } = string.Empty;
    public string InteractionType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionNorwegian { get; set; } = string.Empty;
    public string Management { get; set; } = string.Empty;
    public string ManagementNorwegian { get; set; } = string.Empty;
}

public class AllergyWarning
{
    public string Medication { get; set; } = string.Empty;
    public string Allergen { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string SymptomsNorwegian { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public class DosageRecommendation
{
    public string RecommendedDose { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string FrequencyNorwegian { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string SpecialInstructions { get; set; } = string.Empty;
    public string SpecialInstructionsNorwegian { get; set; } = string.Empty;
    public List<string> Adjustments { get; set; } = new();
}

public class MedicalImageAnalysisRequest
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string ImageType { get; set; } = string.Empty; // X-ray, MRI, CT, Ultrasound, Photo
    public string BodyPart { get; set; } = string.Empty;
    public string? ClinicalContext { get; set; }
    public string? PatientAge { get; set; }
    public string? Gender { get; set; }
}

public class MedicalImageAnalysisResponse
{
    public List<ImageFinding> Findings { get; set; } = new();
    public string OverallAssessment { get; set; } = string.Empty;
    public string OverallAssessmentNorwegian { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
    public bool RequiresSpecialistReview { get; set; }
    public string? SpecialistType { get; set; }
}

public class ImageFinding
{
    public string Finding { get; set; } = string.Empty;
    public string FindingNorwegian { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DescriptionNorwegian { get; set; } = string.Empty;
    public List<string> DifferentialDiagnoses { get; set; } = new();
}

public class SymptomAnalysisRequest
{
    public string Symptoms { get; set; } = string.Empty;
    public string Language { get; set; } = "no";
    public int? PatientAge { get; set; }
    public string? Gender { get; set; }
    public string? MedicalHistory { get; set; }
    public string? CurrentMedications { get; set; }
}

public class SymptomAnalysisResponse
{
    public List<PossibleCondition> PossibleConditions { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public List<string> RecommendedActionsNorwegian { get; set; } = new();
    public List<string> WhenToSeekHelp { get; set; } = new();
    public List<string> WhenToSeekHelpNorwegian { get; set; } = new();
    public UrgencyAssessment Urgency { get; set; } = new();
    public string Disclaimer { get; set; } = string.Empty;
    public string DisclaimerNorwegian { get; set; } = string.Empty;
}

public class PossibleCondition
{
    public string Condition { get; set; } = string.Empty;
    public string ConditionNorwegian { get; set; } = string.Empty;
    public double Probability { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DescriptionNorwegian { get; set; } = string.Empty;
    public List<string> MatchingSymptoms { get; set; } = new();
    public List<string> AdditionalSymptoms { get; set; } = new();
}

public class ConsultationSummaryRequest
{
    public string ConversationTranscript { get; set; } = string.Empty;
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlan { get; set; }
    public string Language { get; set; } = "no";
}

public class ConsultationSummaryResponse
{
    public string Summary { get; set; } = string.Empty;
    public string SummaryNorwegian { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public List<string> KeyPointsNorwegian { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
    public List<string> ActionItemsNorwegian { get; set; } = new();
    public List<string> FollowUpRecommendations { get; set; } = new();
    public List<string> SuggestedICD10Codes { get; set; } = new();
    public SOAPNote SOAPNote { get; set; } = new();
}

public class SOAPNote
{
    public string Subjective { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string SubjectiveNorwegian { get; set; } = string.Empty;
    public string ObjectiveNorwegian { get; set; } = string.Empty;
    public string AssessmentNorwegian { get; set; } = string.Empty;
    public string PlanNorwegian { get; set; } = string.Empty;
}

public class MedicationInteraction
{
    public string Medication1 { get; set; } = string.Empty;
    public string Medication2 { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low, Medium, High
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

