using Bunit;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Web.Components.Chat;

namespace SmartOpsHub.Web.Tests.Components;

public class AgentStatusIndicatorTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    [Fact]
    public void Renders_GreenDot_ForIdleStatus()
    {
        var cut = _ctx.Render<AgentStatusIndicator>(parameters =>
            parameters.Add(p => p.Status, AgentSessionStatus.Idle));

        var indicator = cut.Find(".status-indicator");
        Assert.Contains("status-idle", indicator.ClassList);
    }

    [Fact]
    public void Renders_YellowDot_ForThinkingStatus()
    {
        var cut = _ctx.Render<AgentStatusIndicator>(parameters =>
            parameters.Add(p => p.Status, AgentSessionStatus.Thinking));

        var indicator = cut.Find(".status-indicator");
        Assert.Contains("status-thinking", indicator.ClassList);
    }

    [Fact]
    public void Renders_PulsingClass_ForActiveStates()
    {
        var thinkingCut = _ctx.Render<AgentStatusIndicator>(parameters =>
            parameters.Add(p => p.Status, AgentSessionStatus.Thinking));
        var workingCut = _ctx.Render<AgentStatusIndicator>(parameters =>
            parameters.Add(p => p.Status, AgentSessionStatus.Working));

        Assert.Contains("status-thinking", thinkingCut.Find(".status-indicator").ClassList);
        Assert.Contains("status-working", workingCut.Find(".status-indicator").ClassList);

        // Idle should NOT have an active/pulsing class
        var idleCut = _ctx.Render<AgentStatusIndicator>(parameters =>
            parameters.Add(p => p.Status, AgentSessionStatus.Idle));
        Assert.Contains("status-idle", idleCut.Find(".status-indicator").ClassList);
        Assert.DoesNotContain("status-thinking", idleCut.Find(".status-indicator").ClassList);
        Assert.DoesNotContain("status-working", idleCut.Find(".status-indicator").ClassList);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }
}
