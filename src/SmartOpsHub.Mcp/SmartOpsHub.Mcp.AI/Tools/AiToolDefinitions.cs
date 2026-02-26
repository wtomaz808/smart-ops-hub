using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.AI.Tools;

public static class AiToolDefinitions
{
    public static readonly McpToolDefinition GenerateCompletion = new()
    {
        Name = "ai_generate_completion",
        Description = "Generates a text completion using an AI model",
        InputSchema = """{"type":"object","properties":{"prompt":{"type":"string"},"model":{"type":"string"},"maxTokens":{"type":"integer"},"temperature":{"type":"number"}},"required":["prompt"]}"""
    };

    public static readonly McpToolDefinition GenerateEmbedding = new()
    {
        Name = "ai_generate_embedding",
        Description = "Generates vector embeddings for text",
        InputSchema = """{"type":"object","properties":{"text":{"type":"string"},"model":{"type":"string"}},"required":["text"]}"""
    };

    public static readonly McpToolDefinition AnalyzeImage = new()
    {
        Name = "ai_analyze_image",
        Description = "Analyzes an image using AI vision capabilities",
        InputSchema = """{"type":"object","properties":{"imageUrl":{"type":"string"},"prompt":{"type":"string"}},"required":["imageUrl"]}"""
    };

    public static readonly McpToolDefinition TranscribeSpeech = new()
    {
        Name = "ai_transcribe_speech",
        Description = "Transcribes audio speech to text",
        InputSchema = """{"type":"object","properties":{"audioUrl":{"type":"string"},"language":{"type":"string"}},"required":["audioUrl"]}"""
    };

    public static readonly McpToolDefinition AnalyzeText = new()
    {
        Name = "ai_analyze_text",
        Description = "Analyzes text for sentiment, entities, and key phrases",
        InputSchema = """{"type":"object","properties":{"text":{"type":"string"},"analysisType":{"type":"string","enum":["sentiment","entities","keyPhrases","all"]}},"required":["text"]}"""
    };

    public static IReadOnlyList<McpToolDefinition> All =>
    [
        GenerateCompletion, GenerateEmbedding, AnalyzeImage, TranscribeSpeech, AnalyzeText
    ];
}
