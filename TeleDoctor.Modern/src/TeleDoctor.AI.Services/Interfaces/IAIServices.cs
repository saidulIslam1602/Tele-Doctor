using TeleDoctor.AI.Services.Models;

namespace TeleDoctor.AI.Services.Interfaces;

public interface IClinicalAIService
{
    Task<ClinicalAnalysisResponse> AnalyzeSymptomsAsync(ClinicalAnalysisRequest request);
    Task<ConsultationSummaryResponse> GenerateConsultationSummaryAsync(ConsultationSummaryRequest request);
    Task<string> GeneratePatientExplanationAsync(string diagnosis, string treatment, string language = "no");
    Task<List<string>> SuggestDifferentialDiagnosesAsync(string symptoms, string patientHistory);
    Task<UrgencyAssessment> AssessUrgencyAsync(string symptoms, string patientHistory);
}

public interface IMedicationAIService
{
    Task<MedicationInteractionResponse> CheckMedicationInteractionsAsync(MedicationInteractionRequest request);
    Task<List<string>> SuggestAlternativeMedicationsAsync(string medication, string patientProfile);
    Task<DosageRecommendation> GetDosageRecommendationAsync(string medication, string patientProfile);
    Task<List<string>> AnalyzePrescriptionSafetyAsync(List<string> medications, string patientProfile);
}

public interface IMedicalImageAIService
{
    Task<MedicalImageAnalysisResponse> AnalyzeMedicalImageAsync(MedicalImageAnalysisRequest request);
    Task<string> ExtractTextFromMedicalImageAsync(byte[] imageData);
    Task<List<string>> DetectAbnormalitiesAsync(byte[] imageData, string imageType);
    Task<double> CalculateImageQualityScoreAsync(byte[] imageData);
}

public interface ISymptomCheckerService
{
    Task<SymptomAnalysisResponse> AnalyzeSymptomsAsync(SymptomAnalysisRequest request);
    Task<List<string>> ExtractSymptomsFromTextAsync(string text, string language = "no");
    Task<UrgencyAssessment> AssessSymptomUrgencyAsync(List<string> symptoms);
    Task<List<string>> SuggestQuestionsAsync(List<string> symptoms, string language = "no");
}

public interface INorwegianLanguageService
{
    Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage);
    Task<string> DetectLanguageAsync(string text);
    Task<string> TranslateToNorwegianAsync(string text);
    Task<string> TranslateFromNorwegianAsync(string text, string targetLanguage = "en");
    Task<Dictionary<string, string>> TranslateMedicalTermsAsync(Dictionary<string, string> terms, string targetLanguage);
}

public interface IMedicalAssistantChatService
{
    Task<string> ProcessDoctorQueryAsync(string query, string patientContext, string language = "no");
    Task<string> ProcessPatientQueryAsync(string query, string medicalHistory, string language = "no");
    Task<List<string>> GenerateFollowUpQuestionsAsync(string conversation, string language = "no");
    Task<string> SummarizeConversationAsync(List<string> messages, string language = "no");
    Task<bool> DetectEmergencyAsync(string message);
}

public interface IPredictiveHealthcareService
{
    Task<List<string>> PredictHealthRisksAsync(string patientProfile, string medicalHistory);
    Task<List<string>> RecommendPreventiveMeasuresAsync(string patientProfile);
    Task<double> CalculateReadmissionRiskAsync(string patientProfile, string dischargeInfo);
    Task<List<string>> SuggestScreeningScheduleAsync(int patientAge, string gender, string riskFactors);
}

public interface IWorkflowOptimizationService
{
    Task<List<string>> OptimizeDoctorScheduleAsync(string doctorProfile, List<string> appointments);
    Task<List<string>> SuggestEfficiencyImprovementsAsync(string workflowData);
    Task<List<string>> AutomateRoutineTasksAsync(List<string> tasks);
    Task<string> GenerateReportAsync(string reportType, string data);
}

public interface IVoiceTranscriptionService
{
    Task<string> TranscribeAudioAsync(byte[] audioData, string language = "no");
    Task<ConsultationSummaryResponse> TranscribeAndSummarizeConsultationAsync(byte[] audioData, string language = "no");
    Task<List<string>> ExtractMedicalTermsAsync(string transcription);
    Task<string> FormatMedicalTranscriptionAsync(string rawTranscription);
}

public interface IAIOrchestrationService
{
    Task<string> ProcessComplexMedicalQueryAsync(string query, string context, string language = "no");
    Task<Dictionary<string, object>> RunMultiStepAnalysisAsync(string patientData, List<string> analysisSteps);
    Task<string> GenerateComprehensiveReportAsync(string patientId, string reportType);
    Task<List<string>> GetAIRecommendationsAsync(string scenario, Dictionary<string, object> parameters);
}
