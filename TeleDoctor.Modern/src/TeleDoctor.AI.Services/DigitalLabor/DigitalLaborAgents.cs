using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleDoctor.AI.Services.Configuration;
using TeleDoctor.AI.Services.AgenticFlows;

namespace TeleDoctor.AI.Services.DigitalLabor;

/// <summary>
/// Digital Labor Agents - AI assistants that automate routine healthcare tasks
/// These agents free up healthcare professionals to focus on patient care
/// </summary>

#region Scheduling Agent
public class SchedulingAgent : IHealthcareAgent
{
    public string AgentId => "SchedulingAgent";
    public string AgentName => "Timebestilling AI-assistent";
    public string Capability => "Intelligent appointment scheduling and resource optimization";

    private readonly OpenAIClient _openAIClient;
    private readonly AIConfiguration _config;
    private readonly ILogger<SchedulingAgent> _logger;

    public SchedulingAgent(
        OpenAIClient openAIClient,
        IOptions<AIConfiguration> config,
        ILogger<SchedulingAgent> logger)
    {
        _openAIClient = openAIClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(WorkflowStep step, AgentContext context)
    {
        _logger.LogInformation("SchedulingAgent executing step: {StepName}", step.Name);

        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = step.Name switch
            {
                "FindOptimalSlot" => await FindOptimalAppointmentSlotAsync(context),
                "ConfirmAvailability" => await ConfirmDoctorAvailabilityAsync(context),
                "ResourceAllocation" => await AllocateResourcesAsync(context),
                "ScheduleFollowUp" => await ScheduleFollowUpAppointmentAsync(context),
                _ => throw new NotImplementedException($"Step not implemented: {step.Name}")
            };

            return new AgentExecutionResult
            {
                StepName = step.Name,
                Success = true,
                Output = result,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SchedulingAgent step: {StepName}", step.Name);
            return new AgentExecutionResult
            {
                StepName = step.Name,
                Success = false,
                Error = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<AgentExecutionResult> ExecuteTaskAsync(AgentTask task, AgentContext context)
    {
        return await ExecuteAsync(new WorkflowStep { Name = task.Name }, context);
    }

    public async Task<AgentContribution> ContributeToCollaborationAsync(string goal, AgentWorkspace workspace)
    {
        var contribution = await AnalyzeSchedulingNeedsAsync(goal, workspace);
        
        return new AgentContribution
        {
            AgentId = AgentId,
            Contribution = contribution,
            ConfidenceScore = 0.9,
            ContributedAt = DateTime.UtcNow
        };
    }

    private async Task<Dictionary<string, object>> FindOptimalAppointmentSlotAsync(AgentContext context)
    {
        // AI-powered appointment optimization logic
        var systemPrompt = @"
            Du er en AI-assistent som optimaliserer timebestillinger i norsk helsevesen.
            Analyser pasientens behov, doktorens spesialisering, hastegrad og tilgjengelighet.
            Finn det mest optimale tidspunktet som balanserer pasientens behov og klinikkens effektivitet.
        ";

        var userPrompt = $@"
            Finn optimal time for:
            Pasient: {context.InputContext.GetValueOrDefault("patientInfo", "Ikke oppgitt")}
            Symptomer: {context.InputContext.GetValueOrDefault("symptoms", "Ikke oppgitt")}
            Hastegrad: {context.InputContext.GetValueOrDefault("urgency", "Normal")}
            Foretrukket tid: {context.InputContext.GetValueOrDefault("preferredTime", "Fleksibel")}
            
            Tilgjengelige tider: [Liste med ledige tider]
        ";

        var response = await GetAIResponseAsync(systemPrompt, userPrompt);
        
        return new Dictionary<string, object>
        {
            { "recommendedSlot", response },
            { "alternativeSlots", new List<string>() },
            { "optimizationScore", 0.95 }
        };
    }

    private async Task<Dictionary<string, object>> ConfirmDoctorAvailabilityAsync(AgentContext context)
    {
        return new Dictionary<string, object>
        {
            { "isAvailable", true },
            { "confirmedSlot", context.InputContext.GetValueOrDefault("requestedSlot", "") }
        };
    }

    private async Task<Dictionary<string, object>> AllocateResourcesAsync(AgentContext context)
    {
        return new Dictionary<string, object>
        {
            { "room", "Konsultasjonsrom 3" },
            { "equipment", new List<string> { "EKG", "Blodtrykkapparat" } },
            { "duration", 30 }
        };
    }

    private async Task<Dictionary<string, object>> ScheduleFollowUpAppointmentAsync(AgentContext context)
    {
        var systemPrompt = @"
            Basert på konsultasjonen, vurder om oppfølging er nødvendig og når.
            Følg norske retningslinjer for oppfølging av ulike tilstander.
        ";

        var response = await GetAIResponseAsync(systemPrompt, "Vurder oppfølgingsbehov");
        
        return new Dictionary<string, object>
        {
            { "followUpRequired", true },
            { "recommendedDate", DateTime.UtcNow.AddDays(14) },
            { "reason", response }
        };
    }

    private async Task<string> AnalyzeSchedulingNeedsAsync(string goal, AgentWorkspace workspace)
    {
        return $"Scheduling analysis for: {goal}. Kan optimalisere timebestillinger og ressursallokering.";
    }

    private async Task<string> GetAIResponseAsync(string systemPrompt, string userPrompt)
    {
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
}
#endregion

#region Documentation Agent
public class DocumentationAgent : IHealthcareAgent
{
    public string AgentId => "DocumentationAgent";
    public string AgentName => "Dokumentasjon AI-assistent";
    public string Capability => "Automated clinical documentation and note generation";

    private readonly OpenAIClient _openAIClient;
    private readonly AIConfiguration _config;
    private readonly ILogger<DocumentationAgent> _logger;

    public DocumentationAgent(
        OpenAIClient openAIClient,
        IOptions<AIConfiguration> config,
        ILogger<DocumentationAgent> logger)
    {
        _openAIClient = openAIClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(WorkflowStep step, AgentContext context)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = step.Name switch
            {
                "TranscribeConsultation" => await TranscribeConsultationAsync(context),
                "GenerateSOAPNote" => await GenerateSOAPNoteAsync(context),
                "GenerateDischargeSummary" => await GenerateDischargeSummaryAsync(context),
                "Documentation" => await GenerateDocumentationAsync(context),
                "PrepareDocuments" => await PrepareAppointmentDocumentsAsync(context),
                "DocumentInitialAssessment" => await DocumentInitialAssessmentAsync(context),
                _ => throw new NotImplementedException($"Step not implemented: {step.Name}")
            };

            return new AgentExecutionResult
            {
                StepName = step.Name,
                Success = true,
                Output = result,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DocumentationAgent step: {StepName}", step.Name);
            return new AgentExecutionResult
            {
                StepName = step.Name,
                Success = false,
                Error = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<AgentExecutionResult> ExecuteTaskAsync(AgentTask task, AgentContext context)
    {
        return await ExecuteAsync(new WorkflowStep { Name = task.Name }, context);
    }

    public async Task<AgentContribution> ContributeToCollaborationAsync(string goal, AgentWorkspace workspace)
    {
        return new AgentContribution
        {
            AgentId = AgentId,
            Contribution = "Kan generere all nødvendig medisinsk dokumentasjon automatisk",
            ConfidenceScore = 0.95,
            ContributedAt = DateTime.UtcNow
        };
    }

    private async Task<Dictionary<string, object>> TranscribeConsultationAsync(AgentContext context)
    {
        // AI-powered transcription and summarization
        return new Dictionary<string, object>
        {
            { "transcription", "Full transkript av konsultasjonen..." },
            { "summary", "Sammendrag av konsultasjonen..." }
        };
    }

    private async Task<Dictionary<string, object>> GenerateSOAPNoteAsync(AgentContext context)
    {
        var systemPrompt = @"
            Du er en erfaren norsk lege som lager SOAP-notater (Subjektivt, Objektivt, Analyse, Plan).
            Basert på konsultasjonsdata, generer et strukturert, profesjonelt SOAP-notat på norsk.
            Følg norske standarder for medisinsk dokumentasjon.
        ";

        var consultationData = context.IntermediateResults.GetValueOrDefault("TranscribeConsultation", new Dictionary<string, object>());
        
        var userPrompt = $@"
            Generer SOAP-notat basert på:
            Transkript: {consultationData}
            
            Inkluder:
            - S (Subjektivt): Pasientens beskrivelse av symptomer
            - O (Objektivt): Objektive funn og undersøkelser
            - A (Analyse): Diagnose og vurdering
            - P (Plan): Behandlingsplan og oppfølging
        ";

        var soapNote = await GetAIResponseAsync(systemPrompt, userPrompt);
        
        return new Dictionary<string, object>
        {
            { "soapNote", soapNote },
            { "generatedAt", DateTime.UtcNow },
            { "language", "no" }
        };
    }

    private async Task<Dictionary<string, object>> GenerateDischargeSummaryAsync(AgentContext context)
    {
        var systemPrompt = @"
            Generer et utskrivningsnotat på norsk som inkluderer:
            - Innleggelsesårsak
            - Gjennomførte undersøkelser og behandling
            - Diagnoser med ICD-10 koder
            - Medisinering ved utskrivelse
            - Oppfølgingsplan
            - Pasientveiledning
        ";

        var dischargeSummary = await GetAIResponseAsync(systemPrompt, "Generer utskrivningsnotat");
        
        return new Dictionary<string, object>
        {
            { "dischargeSummary", dischargeSummary },
            { "readyForDistribution", true }
        };
    }

    private async Task<Dictionary<string, object>> GenerateDocumentationAsync(AgentContext context)
    {
        return new Dictionary<string, object>
        {
            { "documentationType", "AdmissionDocumentation" },
            { "documentContent", "Komplett innleggelsesdokumentasjon..." }
        };
    }

    private async Task<Dictionary<string, object>> PrepareAppointmentDocumentsAsync(AgentContext context)
    {
        return new Dictionary<string, object>
        {
            { "patientInstructions", "Forberedelser før konsultasjon..." },
            { "requiredDocuments", new List<string> { "Helsenorge journal", "Medisinoversikt" } }
        };
    }

    private async Task<Dictionary<string, object>> DocumentInitialAssessmentAsync(AgentContext context)
    {
        return new Dictionary<string, object>
        {
            { "initialAssessment", "Første vurdering dokumentert..." },
            { "triageCategory", "Akutt" }
        };
    }

    private async Task<string> GetAIResponseAsync(string systemPrompt, string userPrompt)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            DeploymentName = _config.AzureOpenAI.ChatDeploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.2f, // Lower temperature for medical documentation accuracy
            MaxTokens = 2000
        };

        var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        return response.Value.Choices[0].Message.Content;
    }
}
#endregion

#region Triage Agent
public class TriageAgent : IHealthcareAgent
{
    public string AgentId => "TriageAgent";
    public string AgentName => "Triagering AI-assistent";
    public string Capability => "AI-powered patient triage and urgency assessment";

    private readonly OpenAIClient _openAIClient;
    private readonly AIConfiguration _config;
    private readonly ILogger<TriageAgent> _logger;

    public TriageAgent(
        OpenAIClient openAIClient,
        IOptions<AIConfiguration> config,
        ILogger<TriageAgent> logger)
    {
        _openAIClient = openAIClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(WorkflowStep step, AgentContext context)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = step.Name switch
            {
                "Triage" => await PerformTriageAsync(context),
                "AnalyzeRequest" => await AnalyzeAppointmentRequestAsync(context),
                "AssessUrgency" => await AssessUrgencyAsync(context),
                _ => throw new NotImplementedException($"Step not implemented: {step.Name}")
            };

            return new AgentExecutionResult
            {
                StepName = step.Name,
                Success = true,
                Output = result,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TriageAgent step: {StepName}", step.Name);
            return new AgentExecutionResult
            {
                StepName = step.Name,
                Success = false,
                Error = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<AgentExecutionResult> ExecuteTaskAsync(AgentTask task, AgentContext context)
    {
        return await ExecuteAsync(new WorkflowStep { Name = task.Name }, context);
    }

    public async Task<AgentContribution> ContributeToCollaborationAsync(string goal, AgentWorkspace workspace)
    {
        return new AgentContribution
        {
            AgentId = AgentId,
            Contribution = "Kan vurdere hastegrad og prioritere pasienter basert på kliniske kriterier",
            ConfidenceScore = 0.92,
            ContributedAt = DateTime.UtcNow
        };
    }

    private async Task<Dictionary<string, object>> PerformTriageAsync(AgentContext context)
    {
        var systemPrompt = @"
            Du er en erfaren triage-sykepleier i norsk helsevesen.
            Vurder pasientens tilstand og tildel riktig hastegrad basert på:
            - Symptomer og vitale tegn
            - Sykdomshistorikk
            - Alvorlighetsgrad
            - Norske triageringsretningslinjer
            
            Hastegrad-kategorier:
            1. Rød (Livstruende - Umiddelbar behandling)
            2. Oransje (Alvorlig - 10 minutter)
            3. Gul (Haster - 60 minutter)
            4. Grønn (Mindre alvorlig - 120 minutter)
            5. Blå (Ikke-akutt - 240 minutter)
        ";

        var patientInfo = context.InputContext.GetValueOrDefault("patientInfo", "").ToString();
        var symptoms = context.InputContext.GetValueOrDefault("symptoms", "").ToString();
        
        var userPrompt = $@"
            Pasient: {patientInfo}
            Symptomer: {symptoms}
            Vitale tegn: {context.InputContext.GetValueOrDefault("vitalSigns", "Ikke målt")}
            
            Vurder hastegrad og begrunn vurderingen.
        ";

        var triageAssessment = await GetAIResponseAsync(systemPrompt, userPrompt);
        
        return new Dictionary<string, object>
        {
            { "triageCategory", "Gul" }, // Example
            { "urgencyScore", 0.7 },
            { "assessment", triageAssessment },
            { "recommendedAction", "Konsultasjon innen 60 minutter" },
            { "redFlags", new List<string>() }
        };
    }

    private async Task<Dictionary<string, object>> AnalyzeAppointmentRequestAsync(AgentContext context)
    {
        return new Dictionary<string, object>
        {
            { "requestType", "Standard Consultation" },
            { "urgency", "Normal" },
            { "recommendedSpecialty", "Allmennmedisin" }
        };
    }

    private async Task<Dictionary<string, object>> AssessUrgencyAsync(AgentContext context)
    {
        var systemPrompt = @"
            Vurder hastegrad for akuttmottak basert på:
            - Livstruende symptomer
            - Smertevurdering
            - Vitale tegn
            - Bevissthetsnivå
        ";

        var urgencyAssessment = await GetAIResponseAsync(systemPrompt, "Vurder akutt hastegrad");
        
        return new Dictionary<string, object>
        {
            { "urgencyLevel", "Critical" },
            { "alertStaff", true },
            { "assessment", urgencyAssessment }
        };
    }

    private async Task<string> GetAIResponseAsync(string systemPrompt, string userPrompt)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            DeploymentName = _config.AzureOpenAI.ChatDeploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.1f, // Very low temperature for critical triage decisions
            MaxTokens = 1000
        };

        var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        return response.Value.Choices[0].Message.Content;
    }
}
#endregion

// Additional agents (CommunicationAgent, AdministrativeAgent, ClinicalDecisionAgent) would follow similar patterns...
