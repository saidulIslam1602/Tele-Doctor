using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TeleDoctor.AI.Services.Configuration;

namespace TeleDoctor.AI.Services.AgenticFlows;

/// <summary>
/// Orchestrates multiple AI agents to automate healthcare workflows
/// Implements agentic AI patterns for digital labor in healthcare
/// </summary>
public interface IHealthcareAgentOrchestrator
{
    Task<AgentWorkflowResult> ExecuteWorkflowAsync(string workflowType, Dictionary<string, object> context);
    Task<List<AgentTask>> PlanTasksAsync(string goal, Dictionary<string, object> context);
    Task<AgentExecutionResult> ExecuteAgentTaskAsync(AgentTask task, AgentContext context);
    Task<MultiAgentCollaboration> CoordinateAgentsAsync(List<string> agentIds, string collaborationGoal);
}

public class HealthcareAgentOrchestrator : IHealthcareAgentOrchestrator
{
    private readonly OpenAIClient _openAIClient;
    private readonly AIConfiguration _config;
    private readonly ILogger<HealthcareAgentOrchestrator> _logger;
    private readonly Dictionary<string, IHealthcareAgent> _agents;

    public HealthcareAgentOrchestrator(
        OpenAIClient openAIClient,
        IOptions<AIConfiguration> config,
        ILogger<HealthcareAgentOrchestrator> logger,
        IEnumerable<IHealthcareAgent> agents)
    {
        _openAIClient = openAIClient;
        _config = config.Value;
        _logger = logger;
        _agents = agents.ToDictionary(a => a.AgentId, a => a);
    }

    public async Task<AgentWorkflowResult> ExecuteWorkflowAsync(string workflowType, Dictionary<string, object> context)
    {
        _logger.LogInformation("Executing healthcare workflow: {WorkflowType}", workflowType);

        var workflow = CreateWorkflow(workflowType);
        var workflowContext = new AgentContext
        {
            WorkflowId = Guid.NewGuid().ToString(),
            WorkflowType = workflowType,
            InputContext = context,
            StartedAt = DateTime.UtcNow
        };

        var results = new List<AgentExecutionResult>();

        foreach (var step in workflow.Steps)
        {
            _logger.LogDebug("Executing workflow step: {StepName}", step.Name);

            var agent = _agents[step.AgentId];
            var stepResult = await agent.ExecuteAsync(step, workflowContext);
            
            results.Add(stepResult);
            
            // Update context with step results
            workflowContext.IntermediateResults[step.Name] = stepResult.Output;

            // Check if workflow should continue
            if (!stepResult.Success && step.IsRequired)
            {
                _logger.LogWarning("Required workflow step failed: {StepName}", step.Name);
                break;
            }
        }

        return new AgentWorkflowResult
        {
            WorkflowId = workflowContext.WorkflowId,
            WorkflowType = workflowType,
            Success = results.All(r => r.Success || !workflow.Steps.First(s => s.Name == r.StepName).IsRequired),
            Results = results,
            Duration = DateTime.UtcNow - workflowContext.StartedAt,
            CompletedAt = DateTime.UtcNow
        };
    }

    public async Task<List<AgentTask>> PlanTasksAsync(string goal, Dictionary<string, object> context)
    {
        _logger.LogInformation("Planning tasks for goal: {Goal}", goal);

        var systemPrompt = @"
            Du er en AI-assistent som hjelper med å planlegge oppgaver i norsk helsevesen.
            Basert på målet, lag en strukturert plan med konkrete oppgaver.
            Hver oppgave skal ha:
            - Navn
            - Beskrivelse
            - Hvilken AI-agent som skal utføre den
            - Avhengigheter til andre oppgaver
            - Estimert tid
            
            Tilgjengelige agenter:
            - SchedulingAgent: Håndterer timebestillinger
            - DocumentationAgent: Lager medisinske dokumenter
            - TriageAgent: Vurderer hastegrad
            - CommunicationAgent: Kommunikasjon med pasienter
            - AdministrativeAgent: Administrativt arbeid
            - ClinicalDecisionAgent: Klinisk beslutningsstøtte
        ";

        var userPrompt = $@"
            Mål: {goal}
            Kontekst: {JsonSerializer.Serialize(context)}
            
            Lag en strukturert oppgaveplan på JSON-format.
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
            MaxTokens = 2000
        };

        var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        var content = response.Value.Choices[0].Message.Content;

        var tasks = JsonSerializer.Deserialize<List<AgentTask>>(content) ?? new List<AgentTask>();
        
        _logger.LogInformation("Generated {TaskCount} tasks for goal", tasks.Count);
        return tasks;
    }

    public async Task<AgentExecutionResult> ExecuteAgentTaskAsync(AgentTask task, AgentContext context)
    {
        try
        {
            if (!_agents.TryGetValue(task.AgentId, out var agent))
            {
                throw new InvalidOperationException($"Agent not found: {task.AgentId}");
            }

            var result = await agent.ExecuteTaskAsync(task, context);
            
            _logger.LogInformation("Agent {AgentId} completed task: {TaskName} - Success: {Success}", 
                task.AgentId, task.Name, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent task: {TaskName}", task.Name);
            return new AgentExecutionResult
            {
                TaskId = task.Id,
                StepName = task.Name,
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<MultiAgentCollaboration> CoordinateAgentsAsync(List<string> agentIds, string collaborationGoal)
    {
        _logger.LogInformation("Coordinating {AgentCount} agents for goal: {Goal}", agentIds.Count, collaborationGoal);

        var collaboration = new MultiAgentCollaboration
        {
            CollaborationId = Guid.NewGuid().ToString(),
            Goal = collaborationGoal,
            ParticipatingAgents = agentIds,
            StartedAt = DateTime.UtcNow
        };

        // Create a shared workspace for agents to collaborate
        var sharedWorkspace = new AgentWorkspace
        {
            WorkspaceId = collaboration.CollaborationId,
            SharedData = new Dictionary<string, object>()
        };

        var agentTasks = new List<Task<AgentContribution>>();

        foreach (var agentId in agentIds)
        {
            if (_agents.TryGetValue(agentId, out var agent))
            {
                agentTasks.Add(agent.ContributeToCollaborationAsync(collaborationGoal, sharedWorkspace));
            }
        }

        var contributions = await Task.WhenAll(agentTasks);
        collaboration.Contributions = contributions.ToList();

        // Synthesize contributions into final result
        collaboration.FinalResult = await SynthesizeContributionsAsync(contributions, collaborationGoal);
        collaboration.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation("Agent collaboration completed: {CollaborationId}", collaboration.CollaborationId);
        return collaboration;
    }

    private HealthcareWorkflow CreateWorkflow(string workflowType)
    {
        return workflowType switch
        {
            "PatientAdmission" => CreatePatientAdmissionWorkflow(),
            "AppointmentScheduling" => CreateAppointmentSchedulingWorkflow(),
            "ClinicalDocumentation" => CreateClinicalDocumentationWorkflow(),
            "DischargeProcess" => CreateDischargeProcessWorkflow(),
            "EmergencyTriage" => CreateEmergencyTriageWorkflow(),
            _ => throw new ArgumentException($"Unknown workflow type: {workflowType}")
        };
    }

    private HealthcareWorkflow CreatePatientAdmissionWorkflow()
    {
        return new HealthcareWorkflow
        {
            Name = "Patient Admission",
            Description = "Automated patient admission process",
            Steps = new List<WorkflowStep>
            {
                new() { Name = "Triage", AgentId = "TriageAgent", IsRequired = true },
                new() { Name = "Registration", AgentId = "AdministrativeAgent", IsRequired = true },
                new() { Name = "InitialAssessment", AgentId = "ClinicalDecisionAgent", IsRequired = true },
                new() { Name = "ResourceAllocation", AgentId = "SchedulingAgent", IsRequired = true },
                new() { Name = "Documentation", AgentId = "DocumentationAgent", IsRequired = true },
                new() { Name = "PatientCommunication", AgentId = "CommunicationAgent", IsRequired = false }
            }
        };
    }

    private HealthcareWorkflow CreateAppointmentSchedulingWorkflow()
    {
        return new HealthcareWorkflow
        {
            Name = "Appointment Scheduling",
            Description = "Intelligent appointment scheduling with AI optimization",
            Steps = new List<WorkflowStep>
            {
                new() { Name = "AnalyzeRequest", AgentId = "TriageAgent", IsRequired = true },
                new() { Name = "FindOptimalSlot", AgentId = "SchedulingAgent", IsRequired = true },
                new() { Name = "ConfirmAvailability", AgentId = "SchedulingAgent", IsRequired = true },
                new() { Name = "SendConfirmation", AgentId = "CommunicationAgent", IsRequired = true },
                new() { Name = "PrepareDocuments", AgentId = "DocumentationAgent", IsRequired = false }
            }
        };
    }

    private HealthcareWorkflow CreateClinicalDocumentationWorkflow()
    {
        return new HealthcareWorkflow
        {
            Name = "Clinical Documentation",
            Description = "Automated clinical documentation and note generation",
            Steps = new List<WorkflowStep>
            {
                new() { Name = "TranscribeConsultation", AgentId = "DocumentationAgent", IsRequired = true },
                new() { Name = "GenerateSOAPNote", AgentId = "DocumentationAgent", IsRequired = true },
                new() { Name = "SuggestICD10Codes", AgentId = "ClinicalDecisionAgent", IsRequired = false },
                new() { Name = "ValidateCompliance", AgentId = "AdministrativeAgent", IsRequired = true },
                new() { Name = "StoreInEHR", AgentId = "AdministrativeAgent", IsRequired = true }
            }
        };
    }

    private HealthcareWorkflow CreateDischargeProcessWorkflow()
    {
        return new HealthcareWorkflow
        {
            Name = "Discharge Process",
            Description = "Automated patient discharge workflow",
            Steps = new List<WorkflowStep>
            {
                new() { Name = "GenerateDischargeSummary", AgentId = "DocumentationAgent", IsRequired = true },
                new() { Name = "PrepareInstructions", AgentId = "CommunicationAgent", IsRequired = true },
                new() { Name = "ScheduleFollowUp", AgentId = "SchedulingAgent", IsRequired = false },
                new() { Name = "ProcessBilling", AgentId = "AdministrativeAgent", IsRequired = true },
                new() { Name = "SendToFastlege", AgentId = "CommunicationAgent", IsRequired = true }
            }
        };
    }

    private HealthcareWorkflow CreateEmergencyTriageWorkflow()
    {
        return new HealthcareWorkflow
        {
            Name = "Emergency Triage",
            Description = "AI-powered emergency triage and resource allocation",
            Steps = new List<WorkflowStep>
            {
                new() { Name = "AssessUrgency", AgentId = "TriageAgent", IsRequired = true },
                new() { Name = "AlertStaff", AgentId = "CommunicationAgent", IsRequired = true },
                new() { Name = "AllocateResources", AgentId = "SchedulingAgent", IsRequired = true },
                new() { Name = "ClinicalGuidance", AgentId = "ClinicalDecisionAgent", IsRequired = true },
                new() { Name = "DocumentInitialAssessment", AgentId = "DocumentationAgent", IsRequired = true }
            }
        };
    }

    private async Task<CollaborationResult> SynthesizeContributionsAsync(AgentContribution[] contributions, string goal)
    {
        var systemPrompt = @"
            Du er en koordinator som syntetiserer bidrag fra flere AI-agenter.
            Kombiner bidragene til et sammenhengende, nyttig resultat.
            Identifiser overlappende informasjon og potensielle konflikter.
        ";

        var contributionsText = string.Join("\n\n", contributions.Select((c, i) => 
            $"Agent {i+1} ({c.AgentId}):\n{c.Contribution}"));

        var userPrompt = $@"
            Mål: {goal}
            
            Bidrag fra agenter:
            {contributionsText}
            
            Syntetiser bidragene til et helhetlig resultat.
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
            MaxTokens = 2000
        };

        var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        
        return new CollaborationResult
        {
            SynthesizedOutput = response.Value.Choices[0].Message.Content,
            ContributionCount = contributions.Length,
            ConfidenceScore = 0.85
        };
    }
}

// Supporting interfaces and models
public interface IHealthcareAgent
{
    string AgentId { get; }
    string AgentName { get; }
    string Capability { get; }
    Task<AgentExecutionResult> ExecuteAsync(WorkflowStep step, AgentContext context);
    Task<AgentExecutionResult> ExecuteTaskAsync(AgentTask task, AgentContext context);
    Task<AgentContribution> ContributeToCollaborationAsync(string goal, AgentWorkspace workspace);
}

public class AgentContext
{
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public Dictionary<string, object> InputContext { get; set; } = new();
    public Dictionary<string, object> IntermediateResults { get; set; } = new();
    public DateTime StartedAt { get; set; }
}

public class AgentWorkspace
{
    public string WorkspaceId { get; set; } = string.Empty;
    public Dictionary<string, object> SharedData { get; set; } = new();
}

public class HealthcareWorkflow
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = new();
}

public class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class AgentTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public int EstimatedMinutes { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class AgentExecutionResult
{
    public string TaskId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public Dictionary<string, object> Output { get; set; } = new();
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
}

public class AgentWorkflowResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<AgentExecutionResult> Results { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class MultiAgentCollaboration
{
    public string CollaborationId { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public List<string> ParticipatingAgents { get; set; } = new();
    public List<AgentContribution> Contributions { get; set; } = new();
    public CollaborationResult FinalResult { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class AgentContribution
{
    public string AgentId { get; set; } = string.Empty;
    public string Contribution { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public DateTime ContributedAt { get; set; }
}

public class CollaborationResult
{
    public string SynthesizedOutput { get; set; } = string.Empty;
    public int ContributionCount { get; set; }
    public double ConfidenceScore { get; set; }
}
