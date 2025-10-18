using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeleDoctor.AI.Services.AgenticFlows;
using TeleDoctor.AI.Services.Configuration;
using TeleDoctor.AI.Services.DigitalLabor;
using TeleDoctor.AI.Services.Interfaces;
using TeleDoctor.AI.Services.ModelEvaluation;
using TeleDoctor.AI.Services.RAG;
using TeleDoctor.AI.Services.Services;

namespace TeleDoctor.AI.Services.Extensions;

/// <summary>
/// Extension methods for configuring AI services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all AI services to the dependency injection container
    /// Configures Azure OpenAI, RAG, model evaluation, and agent services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Updated service collection</returns>
    public static IServiceCollection AddAIServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure AI settings
        services.Configure<AIConfiguration>(
            configuration.GetSection(AIConfiguration.SectionName));

        // Register Azure OpenAI client
        var aiConfig = configuration.GetSection(AIConfiguration.SectionName).Get<AIConfiguration>();
        if (aiConfig?.AzureOpenAI != null && !string.IsNullOrEmpty(aiConfig.AzureOpenAI.Endpoint))
        {
            services.AddSingleton(sp =>
            {
                var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AIConfiguration>>().Value;
                return new OpenAIClient(
                    new Uri(config.AzureOpenAI.Endpoint),
                    new Azure.AzureKeyCredential(config.AzureOpenAI.ApiKey));
            });
        }

        // Register Clinical AI Services
        services.AddScoped<IClinicalAIService, ClinicalAIService>();
        services.AddScoped<IMedicationAIService, MedicationAIService>();
        services.AddScoped<IMedicalImageAIService, MedicalImageAIService>();
        services.AddScoped<ISymptomCheckerService, SymptomCheckerService>();
        services.AddScoped<INorwegianLanguageService, NorwegianLanguageService>();
        services.AddScoped<IMedicalAssistantChatService, MedicalAssistantChatService>();
        services.AddScoped<IPredictiveHealthcareService, PredictiveHealthcareService>();
        services.AddScoped<IWorkflowOptimizationService, WorkflowOptimizationService>();
        services.AddScoped<IVoiceTranscriptionService, VoiceTranscriptionService>();
        services.AddScoped<IAIOrchestrationService, AIOrchestrationService>();

        // Register RAG Services
        services.AddScoped<IMedicalRAGService, MedicalRAGService>();
        services.AddScoped<IMedicalKnowledgeBase, MedicalKnowledgeBase>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();

        // Register Model Evaluation Services
        services.AddScoped<IAIModelEvaluationService, AIModelEvaluationService>();
        services.AddScoped<IModelInferenceService, ModelInferenceService>();
        services.AddScoped<INorwegianMedicalValidator, NorwegianMedicalValidator>();
        services.AddScoped<IClinicalKnowledgeBase, ClinicalKnowledgeBase>();

        // Register Agent Services
        services.AddScoped<IHealthcareAgentOrchestrator, HealthcareAgentOrchestrator>();
        services.AddScoped<IHealthcareAgent, SchedulingAgent>();
        services.AddScoped<IHealthcareAgent, DocumentationAgent>();
        services.AddScoped<IHealthcareAgent, TriageAgent>();
        services.AddScoped<IHealthcareAgent, CommunicationAgent>();
        services.AddScoped<IHealthcareAgent, AdministrativeAgent>();
        services.AddScoped<IHealthcareAgent, ClinicalDecisionAgent>();

        // Register all agents as a collection for orchestrator
        services.AddScoped<IEnumerable<IHealthcareAgent>>(sp => new List<IHealthcareAgent>
        {
            sp.GetRequiredService<SchedulingAgent>(),
            sp.GetRequiredService<DocumentationAgent>(),
            sp.GetRequiredService<TriageAgent>(),
            sp.GetRequiredService<CommunicationAgent>(),
            sp.GetRequiredService<AdministrativeAgent>(),
            sp.GetRequiredService<ClinicalDecisionAgent>()
        });

        return services;
    }
}

// Stub implementations for missing services
// These would be fully implemented in production

public class MedicationAIService : IMedicationAIService
{
    public Task<MedicationInteractionResponse> CheckMedicationInteractionsAsync(MedicationInteractionRequest request)
        => Task.FromResult(new MedicationInteractionResponse());
    
    public Task<List<string>> SuggestAlternativeMedicationsAsync(string medication, string patientProfile)
        => Task.FromResult(new List<string>());
    
    public Task<DosageRecommendation> GetDosageRecommendationAsync(string medication, string patientProfile)
        => Task.FromResult(new DosageRecommendation());
    
    public Task<List<string>> AnalyzePrescriptionSafetyAsync(List<string> medications, string patientProfile)
        => Task.FromResult(new List<string>());
}

public class MedicalImageAIService : IMedicalImageAIService
{
    public Task<MedicalImageAnalysisResponse> AnalyzeMedicalImageAsync(MedicalImageAnalysisRequest request)
        => Task.FromResult(new MedicalImageAnalysisResponse());
    
    public Task<string> ExtractTextFromMedicalImageAsync(byte[] imageData)
        => Task.FromResult(string.Empty);
    
    public Task<List<string>> DetectAbnormalitiesAsync(byte[] imageData, string imageType)
        => Task.FromResult(new List<string>());
    
    public Task<double> CalculateImageQualityScoreAsync(byte[] imageData)
        => Task.FromResult(0.85);
}

public class SymptomCheckerService : ISymptomCheckerService
{
    public Task<SymptomAnalysisResponse> AnalyzeSymptomsAsync(SymptomAnalysisRequest request)
        => Task.FromResult(new SymptomAnalysisResponse());
    
    public Task<List<string>> ExtractSymptomsFromTextAsync(string text, string language = "no")
        => Task.FromResult(new List<string>());
    
    public Task<UrgencyAssessment> AssessSymptomUrgencyAsync(List<string> symptoms)
        => Task.FromResult(new UrgencyAssessment());
    
    public Task<List<string>> SuggestQuestionsAsync(List<string> symptoms, string language = "no")
        => Task.FromResult(new List<string>());
}

public class NorwegianLanguageService : INorwegianLanguageService
{
    public Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage)
        => Task.FromResult(text);
    
    public Task<string> DetectLanguageAsync(string text)
        => Task.FromResult("no");
    
    public Task<string> TranslateToNorwegianAsync(string text)
        => Task.FromResult(text);
    
    public Task<string> TranslateFromNorwegianAsync(string text, string targetLanguage = "en")
        => Task.FromResult(text);
    
    public Task<Dictionary<string, string>> TranslateMedicalTermsAsync(Dictionary<string, string> terms, string targetLanguage)
        => Task.FromResult(terms);
}

public class MedicalAssistantChatService : IMedicalAssistantChatService
{
    public Task<string> ProcessDoctorQueryAsync(string query, string patientContext, string language = "no")
        => Task.FromResult("AI response to doctor query");
    
    public Task<string> ProcessPatientQueryAsync(string query, string medicalHistory, string language = "no")
        => Task.FromResult("AI response to patient query");
    
    public Task<List<string>> GenerateFollowUpQuestionsAsync(string conversation, string language = "no")
        => Task.FromResult(new List<string>());
    
    public Task<string> SummarizeConversationAsync(List<string> messages, string language = "no")
        => Task.FromResult("Conversation summary");
    
    public Task<bool> DetectEmergencyAsync(string message)
        => Task.FromResult(false);
}

public class PredictiveHealthcareService : IPredictiveHealthcareService
{
    public Task<List<string>> PredictHealthRisksAsync(string patientProfile, string medicalHistory)
        => Task.FromResult(new List<string>());
    
    public Task<List<string>> RecommendPreventiveMeasuresAsync(string patientProfile)
        => Task.FromResult(new List<string>());
    
    public Task<double> CalculateReadmissionRiskAsync(string patientProfile, string dischargeInfo)
        => Task.FromResult(0.25);
    
    public Task<List<string>> SuggestScreeningScheduleAsync(int patientAge, string gender, string riskFactors)
        => Task.FromResult(new List<string>());
}

public class WorkflowOptimizationService : IWorkflowOptimizationService
{
    public Task<List<string>> OptimizeDoctorScheduleAsync(string doctorProfile, List<string> appointments)
        => Task.FromResult(new List<string>());
    
    public Task<List<string>> SuggestEfficiencyImprovementsAsync(string workflowData)
        => Task.FromResult(new List<string>());
    
    public Task<List<string>> AutomateRoutineTasksAsync(List<string> tasks)
        => Task.FromResult(new List<string>());
    
    public Task<string> GenerateReportAsync(string reportType, string data)
        => Task.FromResult("Generated report");
}

public class VoiceTranscriptionService : IVoiceTranscriptionService
{
    public Task<string> TranscribeAudioAsync(byte[] audioData, string language = "no")
        => Task.FromResult("Transcribed text");
    
    public Task<ConsultationSummaryResponse> TranscribeAndSummarizeConsultationAsync(byte[] audioData, string language = "no")
        => Task.FromResult(new ConsultationSummaryResponse());
    
    public Task<List<string>> ExtractMedicalTermsAsync(string transcription)
        => Task.FromResult(new List<string>());
    
    public Task<string> FormatMedicalTranscriptionAsync(string rawTranscription)
        => Task.FromResult(rawTranscription);
}

public class AIOrchestrationService : IAIOrchestrationService
{
    public Task<string> ProcessComplexMedicalQueryAsync(string query, string context, string language = "no")
        => Task.FromResult("Complex query response");
    
    public Task<Dictionary<string, object>> RunMultiStepAnalysisAsync(string patientData, List<string> analysisSteps)
        => Task.FromResult(new Dictionary<string, object>());
    
    public Task<string> GenerateComprehensiveReportAsync(string patientId, string reportType)
        => Task.FromResult("Comprehensive report");
    
    public Task<List<string>> GetAIRecommendationsAsync(string scenario, Dictionary<string, object> parameters)
        => Task.FromResult(new List<string>());
}
