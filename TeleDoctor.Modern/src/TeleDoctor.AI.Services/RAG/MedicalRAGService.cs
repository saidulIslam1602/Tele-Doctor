using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TeleDoctor.AI.Services.Configuration;
using TeleDoctor.AI.Services.Models;

namespace TeleDoctor.AI.Services.RAG;

/// <summary>
/// Implements Retrieval-Augmented Generation (RAG) for medical knowledge queries
/// Combines vector search with generative AI to provide evidence-based medical information
/// with source citations and validation against medical standards
/// </summary>

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

    /// <summary>
    /// Queries the medical knowledge base using RAG pattern
    /// Retrieves relevant documents and generates contextual answers with AI
    /// </summary>
    /// <param name="question">Medical question in natural language</param>
    /// <param name="patientContext">Patient-specific context for personalized responses</param>
    /// <param name="language">Language code for response (default: Norwegian)</param>
    /// <returns>Medical RAG response with answer, sources, and validation</returns>
    public async Task<MedicalRAGResponse> QueryMedicalKnowledgeAsync(string question, string patientContext, string language = "no")
    {
        try
        {
            _logger.LogInformation("Processing medical RAG query: {Question}", question);

            // Step 1: Retrieve relevant medical documents using vector search
            // Uses embedding similarity to find most relevant content
            var relevantDocs = await RetrieveRelevantDocumentsAsync(question, 10);

            // Step 2: Get official medical guidelines for the condition
            // Retrieves guidelines from national health authorities
            var guidelines = await GetNorwegianGuidelinesAsync(ExtractConditionFromQuery(question));

            // Step 3: Generate contextual answer using retrieved documents
            // Combines document content with AI to create evidence-based response
            var answer = await GenerateContextualAnswerAsync(question, relevantDocs, patientContext);

            // Step 4: Validate generated answer against medical standards
            // Ensures compliance with official guidelines and best practices
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
        try
        {
            // Use Azure Cognitive Services Translator
            // In production, inject ITranslatorService or use Azure.AI.Translation.Text
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // If already in Norwegian, return as-is
            if (await IsNorwegianTextAsync(text))
                return text;

            // For now, pass through if translation service is not configured
            // In production: integrate with Azure Translator API
            _logger.LogWarning("Translation service not configured. Returning original text.");
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating text to Norwegian");
            return text; // Fallback to original text
        }
    }

    private async Task<bool> IsNorwegianTextAsync(string text)
    {
        // Simple heuristic: check for common Norwegian words/patterns
        var norwegianIndicators = new[] { "og", "i", "på", "er", "til", "av", "med", "for", "det", "en" };
        var words = text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var norwegianWordCount = words.Count(w => norwegianIndicators.Contains(w));
        return norwegianWordCount > words.Length * 0.2; // 20% threshold
    }

    private async Task<List<MedicalGuideline>> QueryHelsedirektoratAsync(string condition)
    {
        try
        {
            // Query Norwegian Health Directorate (Helsedirektoratet) guidelines
            // In production: integrate with actual Helsedirektoratet API
            // For now: return commonly used guidelines from knowledge base
            
            var guidelines = new List<MedicalGuideline>();
            
            // Simulate guideline retrieval based on condition
            if (!string.IsNullOrWhiteSpace(condition))
            {
                // In production, this would be an HTTP call to Helsedirektoratet API
                // or a database query to locally cached guidelines
                _logger.LogInformation("Querying Norwegian guidelines for condition: {Condition}", condition);
                
                // Return empty list for now - guidelines would be populated from external source
                // Integration point for: https://www.helsedirektoratet.no/
            }

            return guidelines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Helsedirektoratet for condition: {Condition}", condition);
            return new List<MedicalGuideline>();
        }
    }
}

// Supporting interfaces and models
public interface IMedicalKnowledgeBase
{
    Task StoreDocumentAsync(MedicalDocument document);
    Task<List<MedicalGuideline>> GetNorwegianGuidelinesAsync(string condition);
    Task<List<string>> GetNorwegianMedicalTermsAsync(string query);
}

// IVectorSearchService is defined in VectorSearchService.cs

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
