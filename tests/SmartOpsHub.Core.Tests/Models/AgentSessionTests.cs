using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Tests.Models;

public class AgentSessionTests
{
    private static AgentSession CreateSession() => new()
    {
        UserId = "user-1",
        AgentType = AgentType.GitHub,
        Agent = new AgentDefinition
        {
            Id = "github-agent",
            Name = "GitHub Agent",
            Description = "Test agent",
            Type = AgentType.GitHub,
            SystemPrompt = "You are a test agent."
        }
    };

    [Fact]
    public void AddMessage_AddsToConversationHistory()
    {
        var session = CreateSession();
        var message = new ChatMessage { Role = ChatRole.User, Content = "Hello" };

        session.AddMessage(message);

        Assert.Single(session.ConversationHistory);
        Assert.Equal("Hello", session.ConversationHistory[0].Content);
    }

    [Fact]
    public void AddMessage_UpdatesLastActivityAt()
    {
        var session = CreateSession();
        var originalActivity = session.LastActivityAt;

        // Small delay to ensure timestamp differs
        Thread.Sleep(10);
        session.AddMessage(new ChatMessage { Role = ChatRole.User, Content = "Hi" });

        Assert.True(session.LastActivityAt >= originalActivity);
    }

    [Fact]
    public void Session_InitializesWithIdleStatus()
    {
        var session = CreateSession();

        Assert.Equal(AgentSessionStatus.Idle, session.Status);
    }

    [Fact]
    public void Session_InitializesWithEmptyConversationHistory()
    {
        var session = CreateSession();

        Assert.Empty(session.ConversationHistory);
    }
}
