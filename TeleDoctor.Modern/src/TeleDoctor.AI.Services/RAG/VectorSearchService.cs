using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TeleDoctor.AI.Services.RAG;

/// <summary>
/// Vector search service for semantic similarity search in medical documents
/// Implements efficient vector search using embeddings for RAG pattern
/// </summary>
public interface IVectorSearchService
{
    Task<List<MedicalDocument>> SearchSimilarAsync(float[] embedding, int topK);
    Task IndexDocumentAsync(MedicalDocument document);
    Task<bool> DeleteDocumentAsync(string documentId);
    Task<int> GetDocumentCountAsync();
}

public class VectorSearchService : IVectorSearchService
{
    private readonly ILogger<VectorSearchService> _logger;
    
    // In-memory vector store for demonstration
    // In production, use Azure Cognitive Search, Pinecone, Weaviate, or similar
    private static readonly ConcurrentDictionary<string, MedicalDocument> _vectorStore = new();

    public VectorSearchService(ILogger<VectorSearchService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Searches for documents with similar embeddings
    /// Uses cosine similarity to find semantically related documents
    /// </summary>
    /// <param name="embedding">Query embedding vector</param>
    /// <param name="topK">Number of top results to return</param>
    /// <returns>List of most similar documents ordered by relevance</returns>
    public async Task<List<MedicalDocument>> SearchSimilarAsync(float[] embedding, int topK)
    {
        try
        {
            if (_vectorStore.IsEmpty)
            {
                _logger.LogWarning("Vector store is empty. No documents indexed yet.");
                return new List<MedicalDocument>();
            }

            // Calculate cosine similarity for all documents
            var similarities = _vectorStore.Values
                .Select(doc => new
                {
                    Document = doc,
                    Similarity = CalculateCosineSimilarity(embedding, doc.Embedding)
                })
                .OrderByDescending(x => x.Similarity)
                .Take(topK)
                .ToList();

            // Set relevance scores and return documents
            var results = similarities.Select(s =>
            {
                var doc = s.Document;
                doc.RelevanceScore = s.Similarity;
                return doc;
            }).ToList();

            _logger.LogInformation("Vector search completed. Found {Count} similar documents", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing vector search");
            return new List<MedicalDocument>();
        }
    }

    /// <summary>
    /// Indexes a medical document with its embedding vector
    /// </summary>
    /// <param name="document">Document to index with embedding</param>
    public async Task IndexDocumentAsync(MedicalDocument document)
    {
        try
        {
            if (document.Embedding == null || document.Embedding.Length == 0)
            {
                throw new ArgumentException("Document must have an embedding vector", nameof(document));
            }

            _vectorStore.AddOrUpdate(document.Id, document, (key, old) => document);
            _logger.LogInformation("Indexed document: {DocumentId} - {Title}", document.Id, document.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document: {DocumentId}", document.Id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a document from the vector store
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(string documentId)
    {
        var removed = _vectorStore.TryRemove(documentId, out _);
        if (removed)
        {
            _logger.LogInformation("Deleted document: {DocumentId}", documentId);
        }
        return removed;
    }

    /// <summary>
    /// Gets the total count of indexed documents
    /// </summary>
    public async Task<int> GetDocumentCountAsync()
    {
        return _vectorStore.Count;
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors
    /// Returns value between -1 and 1 (higher means more similar)
    /// </summary>
    private double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("Vectors must have the same dimension");
        }

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
        {
            return 0;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }
}

/// <summary>
/// Medical knowledge base for storing and retrieving medical information
/// </summary>
public class MedicalKnowledgeBase : IMedicalKnowledgeBase
{
    private readonly ILogger<MedicalKnowledgeBase> _logger;
    
    // In-memory storage for demonstration
    // In production, use a proper database
    private static readonly ConcurrentDictionary<string, MedicalDocument> _knowledgeStore = new();
    private static readonly ConcurrentDictionary<string, List<MedicalGuideline>> _guidelinesStore = new();
    private static readonly ConcurrentDictionary<string, List<string>> _medicalTermsStore = new();

    public MedicalKnowledgeBase(ILogger<MedicalKnowledgeBase> logger)
    {
        _logger = logger;
        InitializeKnowledgeBase();
    }

    /// <summary>
    /// Stores a medical document in the knowledge base
    /// </summary>
    public async Task StoreDocumentAsync(MedicalDocument document)
    {
        _knowledgeStore.AddOrUpdate(document.Id, document, (key, old) => document);
        _logger.LogInformation("Stored document in knowledge base: {Title}", document.Title);
    }

    /// <summary>
    /// Retrieves medical guidelines for a specific condition
    /// </summary>
    public async Task<List<MedicalGuideline>> GetNorwegianGuidelinesAsync(string condition)
    {
        if (_guidelinesStore.TryGetValue(condition.ToLower(), out var guidelines))
        {
            return guidelines;
        }

        // Return empty list if no guidelines found
        return new List<MedicalGuideline>();
    }

    /// <summary>
    /// Gets Norwegian medical terminology synonyms for a query
    /// Helps expand search queries with relevant medical terms
    /// </summary>
    public async Task<List<string>> GetNorwegianMedicalTermsAsync(string query)
    {
        var lowerQuery = query.ToLower();
        var terms = new List<string>();

        foreach (var kvp in _medicalTermsStore)
        {
            if (kvp.Key.Contains(lowerQuery) || kvp.Value.Any(v => v.Contains(lowerQuery)))
            {
                terms.AddRange(kvp.Value);
            }
        }

        return terms.Distinct().ToList();
    }

    /// <summary>
    /// Initializes the knowledge base with sample medical information
    /// In production, this would load from a database or external source
    /// </summary>
    private void InitializeKnowledgeBase()
    {
        // Initialize sample medical guidelines
        _guidelinesStore.TryAdd("diabetes", new List<MedicalGuideline>
        {
            new MedicalGuideline
            {
                Id = "GL-DM-001",
                Title = "Type 2 Diabetes Management Guidelines",
                Source = "National Health Authority",
                KeyRecommendation = "HbA1c target <7% for most adults",
                LastUpdated = DateTime.UtcNow.AddDays(-30),
                Conditions = new List<string> { "Diabetes", "Type 2 Diabetes" }
            }
        });

        _guidelinesStore.TryAdd("hypertension", new List<MedicalGuideline>
        {
            new MedicalGuideline
            {
                Id = "GL-HT-001",
                Title = "Hypertension Treatment Guidelines",
                Source = "National Health Authority",
                KeyRecommendation = "Blood pressure target <140/90 mmHg",
                LastUpdated = DateTime.UtcNow.AddDays(-45),
                Conditions = new List<string> { "Hypertension", "High Blood Pressure" }
            }
        });

        // Initialize medical terminology mappings
        _medicalTermsStore.TryAdd("diabetes", new List<string> { "diabetes mellitus", "hyperglycemia", "high blood sugar" });
        _medicalTermsStore.TryAdd("hypertension", new List<string> { "high blood pressure", "elevated blood pressure", "BP" });
        _medicalTermsStore.TryAdd("headache", new List<string> { "cephalalgia", "head pain", "migraine" });

        _logger.LogInformation("Medical knowledge base initialized with sample data");
    }
}
