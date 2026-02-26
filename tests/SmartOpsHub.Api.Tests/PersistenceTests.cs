using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Infrastructure.Data;
using SmartOpsHub.Infrastructure.Services;

namespace SmartOpsHub.Api.Tests;

public class SessionRepositoryTests : IDisposable
{
    private readonly SmartOpsHubDbContext _dbContext;
    private readonly SessionRepository _repository;

    public SessionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SmartOpsHubDbContext>()
            .UseInMemoryDatabase("SessionRepo_" + Guid.NewGuid().ToString("N"))
            .Options;
        var config = new ConfigurationBuilder().Build();
        _dbContext = new SmartOpsHubDbContext(options, config);
        var registry = new AgentRegistryService();
        _repository = new SessionRepository(_dbContext, registry);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveAsync_NewSession_PersistsToDatabase()
    {
        var session = CreateTestSession();

        await _repository.SaveAsync(session);

        var entity = await _dbContext.AgentSessions.FirstOrDefaultAsync(s => s.SessionId == session.SessionId);
        Assert.NotNull(entity);
        Assert.Equal(session.UserId, entity.UserId);
        Assert.Equal("GitHub", entity.AgentType);
    }

    [Fact]
    public async Task SaveAsync_ExistingSession_UpdatesStatus()
    {
        var session = CreateTestSession();
        await _repository.SaveAsync(session);

        session.Status = AgentSessionStatus.Working;
        await _repository.SaveAsync(session);

        var entity = await _dbContext.AgentSessions.FirstOrDefaultAsync(s => s.SessionId == session.SessionId);
        Assert.NotNull(entity);
        Assert.Equal("Working", entity.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingSession_ReturnsSession()
    {
        var session = CreateTestSession();
        await _repository.SaveAsync(session);

        var retrieved = await _repository.GetByIdAsync(session.SessionId);

        Assert.NotNull(retrieved);
        Assert.Equal(session.SessionId, retrieved.SessionId);
        Assert.Equal(session.UserId, retrieved.UserId);
        Assert.Equal(AgentType.GitHub, retrieved.AgentType);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync("non-existent-id");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsUserSessions()
    {
        var session1 = CreateTestSession("user-1");
        var session2 = CreateTestSession("user-1");
        var session3 = CreateTestSession("user-2");
        await _repository.SaveAsync(session1);
        await _repository.SaveAsync(session2);
        await _repository.SaveAsync(session3);

        var results = await _repository.GetByUserIdAsync("user-1");

        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.Equal("user-1", s.UserId));
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatusInDatabase()
    {
        var session = CreateTestSession();
        await _repository.SaveAsync(session);

        await _repository.UpdateStatusAsync(session.SessionId, AgentSessionStatus.Error);

        var entity = await _dbContext.AgentSessions.FirstOrDefaultAsync(s => s.SessionId == session.SessionId);
        Assert.NotNull(entity);
        Assert.Equal("Error", entity.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSessionAndMessages()
    {
        var session = CreateTestSession();
        await _repository.SaveAsync(session);

        _dbContext.ConversationLogs.Add(new ConversationLog
        {
            SessionId = session.SessionId,
            UserId = session.UserId,
            AgentType = "GitHub",
            MessageContent = "test",
            Role = "User",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        await _repository.DeleteAsync(session.SessionId);

        Assert.Null(await _dbContext.AgentSessions.FirstOrDefaultAsync(s => s.SessionId == session.SessionId));
        Assert.Empty(await _dbContext.ConversationLogs.Where(m => m.SessionId == session.SessionId).ToListAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WithMessages_IncludesConversationHistory()
    {
        var session = CreateTestSession();
        await _repository.SaveAsync(session);

        _dbContext.ConversationLogs.Add(new ConversationLog
        {
            SessionId = session.SessionId,
            UserId = session.UserId,
            AgentType = "GitHub",
            MessageContent = "Hello",
            Role = "User",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(session.SessionId);

        Assert.NotNull(retrieved);
        Assert.Single(retrieved.ConversationHistory);
        Assert.Equal("Hello", retrieved.ConversationHistory[0].Content);
    }

    private static AgentSession CreateTestSession(string userId = "test-user")
    {
        return new AgentSession
        {
            UserId = userId,
            AgentType = AgentType.GitHub,
            Agent = new AgentDefinition
            {
                Id = "github",
                Name = "GitHub Agent",
                Type = AgentType.GitHub,
                Description = "GitHub operations",
                SystemPrompt = "You are a GitHub assistant."
            }
        };
    }
}

public class ConversationRepositoryTests : IDisposable
{
    private readonly SmartOpsHubDbContext _dbContext;
    private readonly ConversationRepository _repository;

    public ConversationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SmartOpsHubDbContext>()
            .UseInMemoryDatabase("ConvRepo_" + Guid.NewGuid().ToString("N"))
            .Options;
        var config = new ConfigurationBuilder().Build();
        _dbContext = new SmartOpsHubDbContext(options, config);
        _repository = new ConversationRepository(_dbContext);

        // Seed a session for FK
        _dbContext.AgentSessions.Add(new SessionEntity
        {
            SessionId = "test-session",
            UserId = "test-user",
            AgentType = "GitHub",
            AgentName = "GitHub Agent",
            SystemPrompt = "You are a GitHub assistant.",
            Status = "Idle"
        });
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AddMessageAsync_PersistsMessage()
    {
        var message = new ChatMessage { Role = ChatRole.User, Content = "Hello" };

        await _repository.AddMessageAsync("test-session", message);

        var logs = await _dbContext.ConversationLogs.Where(m => m.SessionId == "test-session").ToListAsync();
        Assert.Single(logs);
        Assert.Equal("Hello", logs[0].MessageContent);
    }

    [Fact]
    public async Task AddMessageAsync_NoSession_DoesNotThrow()
    {
        var message = new ChatMessage { Role = ChatRole.User, Content = "Hello" };

        await _repository.AddMessageAsync("non-existent", message);

        Assert.Empty(await _dbContext.ConversationLogs.ToListAsync());
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsOrderedMessages()
    {
        var msg1 = new ChatMessage { Role = ChatRole.User, Content = "First" };
        var msg2 = new ChatMessage { Role = ChatRole.Assistant, Content = "Second" };
        await _repository.AddMessageAsync("test-session", msg1);
        await _repository.AddMessageAsync("test-session", msg2);

        var messages = await _repository.GetMessagesAsync("test-session");

        Assert.Equal(2, messages.Count);
        Assert.Equal("First", messages[0].Content);
        Assert.Equal("Second", messages[1].Content);
    }

    [Fact]
    public async Task DeleteBySessionAsync_RemovesAllMessages()
    {
        await _repository.AddMessageAsync("test-session", new ChatMessage { Role = ChatRole.User, Content = "A" });
        await _repository.AddMessageAsync("test-session", new ChatMessage { Role = ChatRole.User, Content = "B" });

        await _repository.DeleteBySessionAsync("test-session");

        Assert.Empty(await _dbContext.ConversationLogs.Where(m => m.SessionId == "test-session").ToListAsync());
    }
}
