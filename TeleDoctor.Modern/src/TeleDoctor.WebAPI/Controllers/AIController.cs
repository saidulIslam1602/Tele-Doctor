using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeleDoctor.AI.Services.Interfaces;
using TeleDoctor.AI.Services.Models;
using TeleDoctor.AI.Services.RAG;

namespace TeleDoctor.WebAPI.Controllers;

/// <summary>
/// API controller for AI-powered healthcare services
/// Provides endpoints for clinical AI, RAG queries, and agent workflows
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IClinicalAIService _clinicalAI;
    private readonly IMedicalRAGService _ragService;
    private readonly ILogger<AIController> _logger;

    public AIController(
        IClinicalAIService clinicalAI,
        IMedicalRAGService ragService,
        ILogger<AIController> logger)
    {
        _clinicalAI = clinicalAI;
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes patient symptoms using AI
    /// </summary>
    /// <param name="request">Clinical analysis request</param>
    /// <returns>Clinical analysis response with diagnoses and recommendations</returns>
    [HttpPost("clinical-analysis")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ClinicalAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClinicalAnalysisResponse>> AnalyzeSymptoms(
        [FromBody] ClinicalAnalysisRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Processing clinical analysis request for symptoms: {Symptoms}", 
                request.Symptoms);

            var result = await _clinicalAI.AnalyzeSymptomsAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in clinical analysis");
            return StatusCode(500, "An error occurred during clinical analysis");
        }
    }

    /// <summary>
    /// Generates consultation summary from transcript
    /// </summary>
    /// <param name="request">Consultation summary request</param>
    /// <returns>Generated consultation summary with SOAP notes</returns>
    [HttpPost("consultation-summary")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ConsultationSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultationSummaryResponse>> GenerateConsultationSummary(
        [FromBody] ConsultationSummaryRequest request)
    {
        try
        {
            _logger.LogInformation("Generating consultation summary");

            var result = await _clinicalAI.GenerateConsultationSummaryAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating consultation summary");
            return StatusCode(500, "An error occurred while generating the summary");
        }
    }

    /// <summary>
    /// Queries medical knowledge base using RAG
    /// </summary>
    /// <param name="question">Medical question</param>
    /// <param name="patientContext">Patient context for personalization</param>
    /// <param name="language">Response language (default: Norwegian)</param>
    /// <returns>Evidence-based answer with sources</returns>
    [HttpPost("rag/query")]
    [Authorize(Roles = "Doctor,Patient")]
    [ProducesResponseType(typeof(MedicalRAGResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicalRAGResponse>> QueryMedicalKnowledge(
        [FromBody] string question,
        [FromQuery] string? patientContext = null,
        [FromQuery] string language = "no")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return BadRequest("Question cannot be empty");
            }

            _logger.LogInformation("Processing RAG query: {Question}", question);

            var result = await _ragService.QueryMedicalKnowledgeAsync(
                question, 
                patientContext ?? string.Empty, 
                language);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RAG query");
            return StatusCode(500, "An error occurred while processing the query");
        }
    }

    /// <summary>
    /// Generates patient-friendly explanation of diagnosis and treatment
    /// </summary>
    /// <param name="diagnosis">Medical diagnosis</param>
    /// <param name="treatment">Treatment plan</param>
    /// <param name="language">Language for explanation (default: Norwegian)</param>
    /// <returns>Patient-friendly explanation</returns>
    [HttpPost("patient-explanation")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GeneratePatientExplanation(
        [FromBody] string diagnosis,
        [FromQuery] string treatment,
        [FromQuery] string language = "no")
    {
        try
        {
            var explanation = await _clinicalAI.GeneratePatientExplanationAsync(
                diagnosis, 
                treatment, 
                language);
            
            return Ok(explanation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating patient explanation");
            return StatusCode(500, "An error occurred while generating the explanation");
        }
    }

    /// <summary>
    /// Gets differential diagnosis suggestions
    /// </summary>
    /// <param name="symptoms">Patient symptoms</param>
    /// <param name="patientHistory">Patient medical history</param>
    /// <returns>List of differential diagnoses</returns>
    [HttpPost("differential-diagnosis")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetDifferentialDiagnoses(
        [FromBody] string symptoms,
        [FromQuery] string? patientHistory = null)
    {
        try
        {
            var diagnoses = await _clinicalAI.SuggestDifferentialDiagnosesAsync(
                symptoms, 
                patientHistory ?? string.Empty);
            
            return Ok(diagnoses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting differential diagnoses");
            return StatusCode(500, "An error occurred while processing the request");
        }
    }

    /// <summary>
    /// Assesses urgency of patient condition
    /// </summary>
    /// <param name="symptoms">Patient symptoms</param>
    /// <param name="patientHistory">Patient medical history</param>
    /// <returns>Urgency assessment</returns>
    [HttpPost("assess-urgency")]
    [Authorize(Roles = "Doctor,Coordinator")]
    [ProducesResponseType(typeof(UrgencyAssessment), StatusCodes.Status200OK)]
    public async Task<ActionResult<UrgencyAssessment>> AssessUrgency(
        [FromBody] string symptoms,
        [FromQuery] string? patientHistory = null)
    {
        try
        {
            var assessment = await _clinicalAI.AssessUrgencyAsync(
                symptoms, 
                patientHistory ?? string.Empty);
            
            return Ok(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing urgency");
            return StatusCode(500, "An error occurred while assessing urgency");
        }
    }
}
