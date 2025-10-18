using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TeleDoctor.AI.Services.Configuration;
using TeleDoctor.AI.Services.Interfaces;
using TeleDoctor.AI.Services.Models;

namespace TeleDoctor.AI.Services.Services;

/// <summary>
/// Provides AI-powered clinical decision support services using Azure OpenAI
/// Implements advanced generative AI for symptom analysis, diagnosis suggestions,
/// and automated medical documentation in Norwegian healthcare context
/// </summary>
public class ClinicalAIService : IClinicalAIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly AIConfiguration _config;
    private readonly INorwegianLanguageService _languageService;
    private readonly ILogger<ClinicalAIService> _logger;

    public ClinicalAIService(
        OpenAIClient openAIClient,
        IOptions<AIConfiguration> config,
        INorwegianLanguageService languageService,
        ILogger<ClinicalAIService> logger)
    {
        _openAIClient = openAIClient;
        _config = config.Value;
        _languageService = languageService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes patient symptoms using AI to provide clinical recommendations
    /// </summary>
    /// <param name="request">Clinical analysis request containing symptoms, patient history, and vitals</param>
    /// <returns>Comprehensive clinical analysis with differential diagnoses and recommendations</returns>
    public async Task<ClinicalAnalysisResponse> AnalyzeSymptomsAsync(ClinicalAnalysisRequest request)
    {
        try
        {
            // Get language-specific system prompt for clinical context
            var systemPrompt = GetClinicalAnalysisSystemPrompt(request.Language);
            
            // Build detailed user prompt with patient information
            var userPrompt = BuildClinicalAnalysisPrompt(request);

            // Configure Azure OpenAI chat completion options
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.AzureOpenAI.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Temperature = (float)_config.AzureOpenAI.Temperature,      // Lower temp for medical accuracy
                MaxTokens = _config.AzureOpenAI.MaxTokens,
                NucleusSamplingFactor = (float)_config.AzureOpenAI.TopP
            };

            // Call Azure OpenAI API to get clinical analysis
            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var content = response.Value.Choices[0].Message.Content;

            // Parse AI response into structured clinical analysis
            var analysisResponse = ParseClinicalAnalysisResponse(content, request.Language);
            
            // Translate to Norwegian if needed for bilingual support
            if (request.Language != "no" && _config.Norwegian.EnableAutoTranslation)
            {
                analysisResponse = await TranslateClinicalAnalysisToNorwegian(analysisResponse);
            }

            _logger.LogInformation("Clinical analysis completed for symptoms: {Symptoms}", request.Symptoms);
            return analysisResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing symptoms: {Symptoms}", request.Symptoms);
            throw;
        }
    }

    public async Task<ConsultationSummaryResponse> GenerateConsultationSummaryAsync(ConsultationSummaryRequest request)
    {
        try
        {
            var systemPrompt = GetConsultationSummarySystemPrompt(request.Language);
            var userPrompt = BuildConsultationSummaryPrompt(request);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.AzureOpenAI.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Temperature = (float)_config.AzureOpenAI.Temperature,
                MaxTokens = _config.AzureOpenAI.MaxTokens
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var content = response.Value.Choices[0].Message.Content;

            var summaryResponse = ParseConsultationSummaryResponse(content, request.Language);
            
            // Translate to Norwegian if needed
            if (request.Language != "no" && _config.Norwegian.EnableAutoTranslation)
            {
                summaryResponse = await TranslateConsultationSummaryToNorwegian(summaryResponse);
            }

            _logger.LogInformation("Consultation summary generated successfully");
            return summaryResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating consultation summary");
            throw;
        }
    }

    public async Task<string> GeneratePatientExplanationAsync(string diagnosis, string treatment, string language = "no")
    {
        try
        {
            var systemPrompt = GetPatientExplanationSystemPrompt(language);
            var userPrompt = $@"
                Diagnose: {diagnosis}
                Behandling: {treatment}
                
                Generer en pasientvennlig forklaring som:
                1. Forklarer diagnosen på en enkel måte
                2. Beskriver behandlingsplanen
                3. Inkluderer oppfølgingsinstruksjoner
                4. Bruker enkelt språk som pasienten kan forstå
                5. Er på norsk
            ";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.AzureOpenAI.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Temperature = 0.3f,
                MaxTokens = 1000
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating patient explanation for diagnosis: {Diagnosis}", diagnosis);
            throw;
        }
    }

    public async Task<List<string>> SuggestDifferentialDiagnosesAsync(string symptoms, string patientHistory)
    {
        try
        {
            var systemPrompt = @"
                Du er en erfaren norsk lege som gir differensialdiagnoser.
                Gi en liste med mulige diagnoser basert på symptomer og pasienthistorie.
                Ranger dem etter sannsynlighet.
                Svar kun med en JSON-liste av diagnoser.
            ";

            var userPrompt = $@"
                Symptomer: {symptoms}
                Pasienthistorie: {patientHistory}
                
                Gi 3-5 differensialdiagnoser rangert etter sannsynlighet.
            ";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.AzureOpenAI.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Temperature = 0.2f,
                MaxTokens = 500
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var content = response.Value.Choices[0].Message.Content;

            // Parse JSON response to list of strings
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting differential diagnoses");
            return new List<string>();
        }
    }

    public async Task<UrgencyAssessment> AssessUrgencyAsync(string symptoms, string patientHistory)
    {
        try
        {
            var systemPrompt = @"
                Du er en erfaren norsk lege som vurderer hastegrad av symptomer.
                Vurder hastegraden på en skala fra 0-1 hvor:
                0-0.3 = Lav hastegrad
                0.3-0.6 = Moderat hastegrad  
                0.6-0.8 = Høy hastegrad
                0.8-1.0 = Kritisk hastegrad
                
                Svar med JSON format med score, level, reasoning og timeframe.
            ";

            var userPrompt = $@"
                Symptomer: {symptoms}
                Pasienthistorie: {patientHistory}
                
                Vurder hastegraden og gi anbefaling for tidsramme.
            ";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.AzureOpenAI.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Temperature = 0.1f,
                MaxTokens = 300
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var content = response.Value.Choices[0].Message.Content;

            return JsonSerializer.Deserialize<UrgencyAssessment>(content) ?? new UrgencyAssessment();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing urgency");
            return new UrgencyAssessment { Level = "Medium", Score = 0.5, Reasoning = "Kunne ikke vurdere hastegrad" };
        }
    }

    private string GetClinicalAnalysisSystemPrompt(string language)
    {
        return language == "no" ? @"
            Du er en erfaren norsk lege med spesialisering i klinisk beslutningsstøtte.
            Du hjelper leger med å analysere symptomer og gi differensialdiagnoser.
            
            Dine svar skal være:
            - Evidensbaserte og følge norske retningslinjer
            - Strukturerte og klare
            - Inkludere ICD-10 koder når relevant
            - På norsk
            - Inkludere advarsler om røde flagg
            
            Husk alltid å understreke at dette er beslutningsstøtte og ikke erstatter klinisk vurdering.
        " : @"
            You are an experienced Norwegian physician specializing in clinical decision support.
            You help doctors analyze symptoms and provide differential diagnoses.
            
            Your responses should be:
            - Evidence-based and follow Norwegian guidelines
            - Structured and clear
            - Include ICD-10 codes when relevant
            - Include red flag warnings
            
            Always emphasize that this is decision support and does not replace clinical judgment.
        ";
    }

    private string GetConsultationSummarySystemPrompt(string language)
    {
        return language == "no" ? @"
            Du er en erfaren norsk lege som lager konsultasjonssammendrag.
            Lag strukturerte SOAP-notater basert på konsultasjonssamtalen.
            
            Inkluder:
            - Subjektive funn (S)
            - Objektive funn (O) 
            - Vurdering/Assessment (A)
            - Plan (P)
            - ICD-10 koder
            - Oppfølgingsanbefalinger
        " : @"
            You are an experienced Norwegian physician creating consultation summaries.
            Create structured SOAP notes based on the consultation conversation.
            
            Include:
            - Subjective findings (S)
            - Objective findings (O)
            - Assessment (A)
            - Plan (P)
            - ICD-10 codes
            - Follow-up recommendations
        ";
    }

    private string GetPatientExplanationSystemPrompt(string language)
    {
        return @"
            Du er en erfaren norsk lege som forklarer medisinske tilstander til pasienter.
            Bruk enkelt språk som pasienter kan forstå.
            Unngå kompliserte medisinske termer.
            Vær empatisk og beroligende.
            Gi praktiske råd og oppfølgingsinstruksjoner.
        ";
    }

    private string BuildClinicalAnalysisPrompt(ClinicalAnalysisRequest request)
    {
        return $@"
            Analyser følgende pasientinformasjon og gi klinisk vurdering:
            
            Symptomer: {request.Symptoms}
            Pasienthistorie: {request.PatientHistory}
            Nåværende medisiner: {request.CurrentMedications ?? "Ingen oppgitt"}
            Allergier: {request.Allergies ?? "Ingen kjente"}
            Alder: {request.PatientAge}
            Kjønn: {request.Gender ?? "Ikke oppgitt"}
            Vitale tegn: {request.VitalSigns ?? "Ikke målt"}
            
            Gi en strukturert analyse med:
            1. Differensialdiagnoser (rangert etter sannsynlighet)
            2. Anbefalte undersøkelser
            3. Røde flagg å være oppmerksom på
            4. Oppfølgingsanbefalinger
            5. Hastegrad vurdering
            
            Svar i JSON format.
        ";
    }

    private string BuildConsultationSummaryPrompt(ConsultationSummaryRequest request)
    {
        return $@"
            Lag et konsultasjonssammendrag basert på følgende informasjon:
            
            Samtaletranskript: {request.ConversationTranscript}
            Symptomer: {request.Symptoms ?? "Se transkript"}
            Diagnose: {request.Diagnosis ?? "Ikke fastsatt"}
            Behandlingsplan: {request.TreatmentPlan ?? "Se transkript"}
            
            Lag strukturerte SOAP-notater og sammendrag i JSON format.
        ";
    }

    private ClinicalAnalysisResponse ParseClinicalAnalysisResponse(string content, string language)
    {
        try
        {
            return JsonSerializer.Deserialize<ClinicalAnalysisResponse>(content) ?? new ClinicalAnalysisResponse();
        }
        catch
        {
            // Fallback if JSON parsing fails
            return new ClinicalAnalysisResponse
            {
                Summary = content,
                ConfidenceScore = 0.5
            };
        }
    }

    private ConsultationSummaryResponse ParseConsultationSummaryResponse(string content, string language)
    {
        try
        {
            return JsonSerializer.Deserialize<ConsultationSummaryResponse>(content) ?? new ConsultationSummaryResponse();
        }
        catch
        {
            // Fallback if JSON parsing fails
            return new ConsultationSummaryResponse
            {
                Summary = content
            };
        }
    }

    private async Task<ClinicalAnalysisResponse> TranslateClinicalAnalysisToNorwegian(ClinicalAnalysisResponse response)
    {
        if (!string.IsNullOrEmpty(response.Summary))
        {
            response.SummaryNorwegian = await _languageService.TranslateToNorwegianAsync(response.Summary);
        }

        foreach (var diagnosis in response.DifferentialDiagnoses)
        {
            if (!string.IsNullOrEmpty(diagnosis.Diagnosis))
            {
                diagnosis.DiagnosisNorwegian = await _languageService.TranslateToNorwegianAsync(diagnosis.Diagnosis);
            }
            if (!string.IsNullOrEmpty(diagnosis.Reasoning))
            {
                diagnosis.ReasoningNorwegian = await _languageService.TranslateToNorwegianAsync(diagnosis.Reasoning);
            }
        }

        if (!string.IsNullOrEmpty(response.Urgency.Reasoning))
        {
            response.Urgency.ReasoningNorwegian = await _languageService.TranslateToNorwegianAsync(response.Urgency.Reasoning);
        }

        return response;
    }

    private async Task<ConsultationSummaryResponse> TranslateConsultationSummaryToNorwegian(ConsultationSummaryResponse response)
    {
        if (!string.IsNullOrEmpty(response.Summary))
        {
            response.SummaryNorwegian = await _languageService.TranslateToNorwegianAsync(response.Summary);
        }

        response.KeyPointsNorwegian = new List<string>();
        foreach (var keyPoint in response.KeyPoints)
        {
            response.KeyPointsNorwegian.Add(await _languageService.TranslateToNorwegianAsync(keyPoint));
        }

        response.ActionItemsNorwegian = new List<string>();
        foreach (var actionItem in response.ActionItems)
        {
            response.ActionItemsNorwegian.Add(await _languageService.TranslateToNorwegianAsync(actionItem));
        }

        // Translate SOAP notes
        if (response.SOAPNote != null)
        {
            response.SOAPNote.SubjectiveNorwegian = await _languageService.TranslateToNorwegianAsync(response.SOAPNote.Subjective);
            response.SOAPNote.ObjectiveNorwegian = await _languageService.TranslateToNorwegianAsync(response.SOAPNote.Objective);
            response.SOAPNote.AssessmentNorwegian = await _languageService.TranslateToNorwegianAsync(response.SOAPNote.Assessment);
            response.SOAPNote.PlanNorwegian = await _languageService.TranslateToNorwegianAsync(response.SOAPNote.Plan);
        }

        return response;
    }
}
