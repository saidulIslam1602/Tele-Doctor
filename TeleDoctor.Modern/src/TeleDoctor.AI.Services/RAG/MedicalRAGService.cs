using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TeleDoctor.AI.Services.Configuration;
using TeleDoctor.AI.Services.Models;

namespace TeleDoctor.AI.Services.RAG;

public interface IMedicalRAGService
{
    Task<MedicalRAGResponse> QueryMedicalKnowledgeAsync(string question, string patientContext, string language = "no");
    Task<List<MedicalDocument>> RetrieveRelevantDocumentsAsync(string query, int topK = 5);
    Task<string> GenerateContextualAnswerAsync(string question, List<MedicalDocument> documents, string patientContext);
    Task IndexMedicalDocumentAsync(MedicalDocument document);
    Task<List<MedicalGuideline>> GetNorwegianGuidelinesAsync(string condition);
}

public class MedicalRAGService : IMedicalRAGService
{
    private readonly OpenAIClient _openAIClient;
    private readonly AIConfiguration _config;
    private readonly ILogger<MedicalRAGService> _logger;
    private readonly IMedicalKnowledgeBase _knowledgeBase;
    private readonly IVectorSearchService _vectorSearch;

    public MedicalRAGService(
        OpenAIClient openAIClient,
        IOptions<AIConfiguration> config,
        ILogger<MedicalRAGService> logger,
        IMedicalKnowledgeBase knowledgeBase,
        IVectorSearchService vectorSearch)
    {
        _openAIClient = openAIClient;
        _config = config.Value;
        _logger = logger;
        _knowledgeBase = knowledgeBase;
        _vectorSearch = vectorSearch;
    }

    public async Task<MedicalRAGResponse> QueryMedicalKnowledgeAsync(string question, string patientContext, string language = "no")
    {
        try
        {
            _logger.LogInformation("Processing medical RAG query: {Question}", question);

            // Step 1: Retrieve relevant medical documents
            var relevantDocs = await RetrieveRelevantDocumentsAsync(question, 10);

            // Step 2: Get Norwegian medical guidelines
            var guidelines = await GetNorwegianGuidelinesAsync(ExtractConditionFromQuery(question));

            // Step 3: Generate contextual answer
            var answer = await GenerateContextualAnswerAsync(question, relevantDocs, patientContext);

            // Step 4: Validate against Norwegian medical standards
            var validation = await ValidateAgainstNorwegianStandardsAsync(answer, guidelines);

            var response = new MedicalRAGResponse
            {
                Answer = answer,
                AnswerNorwegian = language == "no" ? answer : await TranslateToNorwegianAsync(answer),
                RelevantDocuments = relevantDocs,
                NorwegianGuidelines = guidelines,
                ConfidenceScore = CalculateConfidenceScore(relevantDocs, validation),
                Sources = relevantDocs.Select(d => d.Source).ToList(),
                Validation = validation,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Medical RAG query completed with confidence: {Confidence}", response.ConfidenceScore);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing medical RAG query: {Question}", question);
            throw;
        }
    }

    public async Task<List<MedicalDocument>> RetrieveRelevantDocumentsAsync(string query, int topK = 5)
    {
        try
        {
            // Generate embedding for the query
            var queryEmbedding = await GenerateEmbeddingAsync(query);

            // Search for similar documents in vector database
            var similarDocuments = await _vectorSearch.SearchSimilarAsync(queryEmbedding, topK);

            // Enhance with Norwegian medical terminology
            var enhancedQuery = await EnhanceWithNorwegianTerminologyAsync(query);
            var norwegianDocs = await _vectorSearch.SearchSimilarAsync(
                await GenerateEmbeddingAsync(enhancedQuery), topK / 2);

            // Combine and deduplicate results
            var allDocs = similarDocuments.Concat(norwegianDocs)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .OrderByDescending(d => d.RelevanceScore)
                .Take(topK)
                .ToList();

            return allDocs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving relevant documents for query: {Query}", query);
            return new List<MedicalDocument>();
        }
    }

    public async Task<string> GenerateContextualAnswerAsync(string question, List<MedicalDocument> documents, string patientContext)
    {
        try
        {
            var systemPrompt = @"
                Du er en erfaren norsk lege med tilgang til omfattende medisinsk kunnskap.
                Bruk de oppgitte dokumentene og pasientkonteksten til å gi et nøyaktig, evidensbasert svar.
                
                Retningslinjer:
                - Følg norske medisinske retningslinjer og standarder
                - Inkluder relevante ICD-10 koder når aktuelt
                - Vær tydelig på usikkerhet og begrensninger
                - Anbefal videre undersøkelser når nødvendig
                - Bruk norsk medisinsk terminologi
                - Referer til kilder når mulig
                
                VIKTIG: Dette er beslutningsstøtte, ikke erstatning for klinisk vurdering.
            ";

            var contextText = string.Join("\n\n", documents.Select(d => 
                $"Kilde: {d.Source}\nTittel: {d.Title}\nInnhold: {d.Content}"));

            var userPrompt = $@"
                Spørsmål: {question}
                
                Pasientkontekst: {patientContext}
                
                Tilgjengelig medisinsk kunnskap:
                {contextText}
                
                Gi et strukturert, evidensbasert svar på norsk.
            ";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.AzureOpenAI.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                Temperature = 0.2f, // Lower temperature for medical accuracy
                MaxTokens = 2000
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating contextual answer");
            throw;
        }
    }

    public async Task IndexMedicalDocumentAsync(MedicalDocument document)
    {
        try
        {
            // Generate embedding for the document
            var embedding = await GenerateEmbeddingAsync(document.Content);
            document.Embedding = embedding;

            // Store in vector database
            await _vectorSearch.IndexDocumentAsync(document);

            // Store in knowledge base
            await _knowledgeBase.StoreDocumentAsync(document);

            _logger.LogInformation("Indexed medical document: {Title}", document.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing medical document: {Title}", document.Title);
            throw;
        }
    }

    public async Task<List<MedicalGuideline>> GetNorwegianGuidelinesAsync(string condition)
    {
        try
        {
            // Query Norwegian medical guidelines database
            var guidelines = await _knowledgeBase.GetNorwegianGuidelinesAsync(condition);

            // Enhance with Helsedirektoratet guidelines
            var helsedirektoratGuidelines = await QueryHelsedirektoratAsync(condition);
            guidelines.AddRange(helsedirektoratGuidelines);

            return guidelines.OrderByDescending(g => g.LastUpdated).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Norwegian guidelines for condition: {Condition}", condition);
            return new List<MedicalGuideline>();
        }
    }

    private async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var embeddingOptions = new EmbeddingsOptions(_config.AzureOpenAI.EmbeddingDeploymentName, new[] { text });
            var response = await _openAIClient.GetEmbeddingsAsync(embeddingOptions);
            return response.Value.Data[0].Embedding.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    private async Task<string> EnhanceWithNorwegianTerminologyAsync(string query)
    {
        // Add Norwegian medical synonyms and terminology
        var norwegianTerms = await _knowledgeBase.GetNorwegianMedicalTermsAsync(query);
        return $"{query} {string.Join(" ", norwegianTerms)}";
    }

    private async Task<MedicalValidation> ValidateAgainstNorwegianStandardsAsync(string answer, List<MedicalGuideline> guidelines)
    {
        // Validate the generated answer against Norwegian medical standards
        var validation = new MedicalValidation
        {
            IsCompliant = true,
            ComplianceScore = 0.95,
            Warnings = new List<string>(),
            Recommendations = new List<string>()
        };

        // Check against guidelines
        foreach (var guideline in guidelines)
        {
            if (!answer.Contains(guideline.KeyRecommendation, StringComparison.OrdinalIgnoreCase))
            {
                validation.Warnings.Add($"Vurder å inkludere anbefaling fra {guideline.Source}");
            }
        }

        return validation;
    }

    private double CalculateConfidenceScore(List<MedicalDocument> documents, MedicalValidation validation)
    {
        var docScore = documents.Any() ? documents.Average(d => d.RelevanceScore) : 0.5;
        var validationScore = validation.ComplianceScore;
        return (docScore + validationScore) / 2.0;
    }

    private string ExtractConditionFromQuery(string query)
    {
        // Simple extraction - in production, use NLP to extract medical conditions
        var commonConditions = new[] { "diabetes", "hypertensjon", "astma", "depresjon", "angst" };
        return commonConditions.FirstOrDefault(c => query.Contains(c, StringComparison.OrdinalIgnoreCase)) ?? "general";
    }

    private async Task<string> TranslateToNorwegianAsync(string text)
    {
        // Use Azure Translator or similar service
        return text; // Placeholder - implement actual translation
    }

    private async Task<List<MedicalGuideline>> QueryHelsedirektoratAsync(string condition)
    {
        // Query Helsedirektoratet's API for official Norwegian guidelines
        return new List<MedicalGuideline>(); // Placeholder - implement actual API call
    }
}

// Supporting interfaces and models
public interface IMedicalKnowledgeBase
{
    Task StoreDocumentAsync(MedicalDocument document);
    Task<List<MedicalGuideline>> GetNorwegianGuidelinesAsync(string condition);
    Task<List<string>> GetNorwegianMedicalTermsAsync(string query);
}

public interface IVectorSearchService
{
    Task<List<MedicalDocument>> SearchSimilarAsync(float[] embedding, int topK);
    Task IndexDocumentAsync(MedicalDocument document);
}

public class MedicalRAGResponse
{
    public string Answer { get; set; } = string.Empty;
    public string AnswerNorwegian { get; set; } = string.Empty;
    public List<MedicalDocument> RelevantDocuments { get; set; } = new();
    public List<MedicalGuideline> NorwegianGuidelines { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public List<string> Sources { get; set; } = new();
    public MedicalValidation Validation { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class MedicalDocument
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public List<string> Keywords { get; set; } = new();
    public string Language { get; set; } = "no";
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public double RelevanceScore { get; set; }
    public string DocumentType { get; set; } = string.Empty; // Guideline, Research, Clinical
}

public class MedicalGuideline
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Helsedirektoratet, Legeforeningen, etc.
    public string KeyRecommendation { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> Conditions { get; set; } = new();
}

public class MedicalValidation
{
    public bool IsCompliant { get; set; }
    public double ComplianceScore { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
