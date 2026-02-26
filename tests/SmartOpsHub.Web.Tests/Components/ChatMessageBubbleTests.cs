using Bunit;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Web.Components.Chat;

namespace SmartOpsHub.Web.Tests.Components;

public class ChatMessageBubbleTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    [Fact]
    public void Renders_UserMessage_WithUserClass()
    {
        var message = new ChatMessage { Role = ChatRole.User, Content = "Hello agent" };
        var cut = _ctx.Render<ChatMessageBubble>(p => p.Add(x => x.Message, message));

        var bubble = cut.Find(".chat-bubble");
        Assert.Contains("chat-bubble-user", bubble.ClassList);
        Assert.DoesNotContain("chat-bubble-assistant", bubble.ClassList);
    }

    [Fact]
    public void Renders_AssistantMessage_WithAssistantClass()
    {
        var message = new ChatMessage { Role = ChatRole.Assistant, Content = "Hi there" };
        var cut = _ctx.Render<ChatMessageBubble>(p => p.Add(x => x.Message, message));

        var bubble = cut.Find(".chat-bubble");
        Assert.Contains("chat-bubble-assistant", bubble.ClassList);
        Assert.DoesNotContain("chat-bubble-user", bubble.ClassList);
    }

    [Fact]
    public void Renders_MessageContent()
    {
        var message = new ChatMessage { Role = ChatRole.User, Content = "Test content here" };
        var cut = _ctx.Render<ChatMessageBubble>(p => p.Add(x => x.Message, message));

        var content = cut.Find(".chat-bubble-content");
        Assert.Contains("Test content here", content.TextContent);
    }

    [Fact]
    public void Renders_Timestamp()
    {
        var message = new ChatMessage { Role = ChatRole.User, Content = "Timed" };
        var cut = _ctx.Render<ChatMessageBubble>(p => p.Add(x => x.Message, message));

        var time = cut.Find(".chat-bubble-time");
        Assert.False(string.IsNullOrWhiteSpace(time.TextContent));
    }

    [Fact]
    public void Different_Roles_Render_Different_Styles()
    {
        var userMsg = new ChatMessage { Role = ChatRole.User, Content = "user" };
        var assistantMsg = new ChatMessage { Role = ChatRole.Assistant, Content = "assistant" };

        var userCut = _ctx.Render<ChatMessageBubble>(p => p.Add(x => x.Message, userMsg));
        var assistantCut = _ctx.Render<ChatMessageBubble>(p => p.Add(x => x.Message, assistantMsg));

        var userClasses = userCut.Find(".chat-bubble").ClassList;
        var assistantClasses = assistantCut.Find(".chat-bubble").ClassList;

        Assert.NotEqual(userClasses, assistantClasses);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }
}
