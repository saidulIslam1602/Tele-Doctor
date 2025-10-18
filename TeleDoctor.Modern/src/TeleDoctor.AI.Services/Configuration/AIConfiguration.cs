namespace TeleDoctor.AI.Services.Configuration;

public class AIConfiguration
{
    public const string SectionName = "AI";
    
    public AzureOpenAIConfiguration AzureOpenAI { get; set; } = new();
    public AzureCognitiveServicesConfiguration CognitiveServices { get; set; } = new();
    public NorwegianAIConfiguration Norwegian { get; set; } = new();
    public MedicalAIConfiguration Medical { get; set; } = new();
}

public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4";
    public string ChatDeploymentName { get; set; } = "gpt-4";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";
    public int MaxTokens { get; set; } = 4000;
    public double Temperature { get; set; } = 0.3;
    public double TopP { get; set; } = 0.95;
    public int MaxRetries { get; set; } = 3;
}

public class AzureCognitiveServicesConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Region { get; set; } = "norwayeast";
    public TextAnalyticsConfiguration TextAnalytics { get; set; } = new();
    public TranslatorConfiguration Translator { get; set; } = new();
    public ComputerVisionConfiguration ComputerVision { get; set; } = new();
}

public class TextAnalyticsConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class TranslatorConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Region { get; set; } = "norwayeast";
    public string DefaultSourceLanguage { get; set; } = "no";
    public string DefaultTargetLanguage { get; set; } = "en";
}

public class ComputerVisionConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class NorwegianAIConfiguration
{
    public bool EnableNorwegianLanguageSupport { get; set; } = true;
    public bool EnableAutoTranslation { get; set; } = true;
    public string PreferredLanguage { get; set; } = "no";
    public bool EnableHelsenorgeIntegration { get; set; } = true;
    public string HelsenorgeApiEndpoint { get; set; } = string.Empty;
    public string HelsenorgeApiKey { get; set; } = string.Empty;
}

public class MedicalAIConfiguration
{
    public bool EnableClinicalDecisionSupport { get; set; } = true;
    public bool EnableSymptomAnalysis { get; set; } = true;
    public bool EnableMedicationInteractionCheck { get; set; } = true;
    public bool EnableMedicalImageAnalysis { get; set; } = true;
    public bool EnablePredictiveAnalytics { get; set; } = true;
    public double ConfidenceThreshold { get; set; } = 0.7;
    public int MaxDifferentialDiagnoses { get; set; } = 5;
    public bool RequireDoctorApproval { get; set; } = true;
}
