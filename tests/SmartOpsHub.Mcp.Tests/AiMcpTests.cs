using System.Text.Json;
using SmartOpsHub.Mcp.AI.Tools;

namespace SmartOpsHub.Mcp.Tests;

public class AiToolDefinitionsTests
{
    [Fact]
    public void All_Returns_Five_Tools()
    {
        Assert.Equal(5, AiToolDefinitions.All.Count);
    }

    [Theory]
    [InlineData("ai_generate_completion")]
    [InlineData("ai_generate_embedding")]
    [InlineData("ai_analyze_image")]
    [InlineData("ai_transcribe_speech")]
    [InlineData("ai_analyze_text")]
    public void All_Contains_Tool(string toolName)
    {
        Assert.Contains(AiToolDefinitions.All, t => t.Name == toolName);
    }

    [Fact]
    public void All_Tools_Have_Valid_Schemas()
    {
        foreach (var tool in AiToolDefinitions.All)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
            var doc = JsonDocument.Parse(tool.InputSchema!);
            Assert.Equal("object", doc.RootElement.GetProperty("type").GetString());
        }
    }

    [Fact]
    public void GenerateCompletion_Requires_Prompt()
    {
        var schema = JsonDocument.Parse(AiToolDefinitions.GenerateCompletion.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("prompt", required);
    }

    [Fact]
    public void AnalyzeText_Has_AnalysisType_Enum()
    {
        var schema = JsonDocument.Parse(AiToolDefinitions.AnalyzeText.InputSchema!).RootElement;
        var analysisType = schema.GetProperty("properties").GetProperty("analysisType");
        var enumValues = analysisType.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("sentiment", enumValues);
        Assert.Contains("entities", enumValues);
        Assert.Contains("keyPhrases", enumValues);
        Assert.Contains("all", enumValues);
    }
}
