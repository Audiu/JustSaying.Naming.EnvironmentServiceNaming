using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using Serilog;

namespace JustSaying.Naming.IntegrationTests.NamingTest.Messages;

public class TestMessageHandler : IHandlerAsync<TestMessageBase>
{
    private readonly ILogger _logger;
    private readonly IMessageContextAccessor _messageContextAccessor;

    public static string LastUniqueId { get; private set; }
    public static Uri LastQueueUri { get; private set; }

    public TestMessageHandler(ILogger logger, IMessageContextAccessor messageContextAccessor)
    {
        _logger = logger;
        _messageContextAccessor = messageContextAccessor;
    }

    public async Task<bool> Handle(TestMessageBase messageBase)
    {
        LastUniqueId = messageBase.UniqueId;
        LastQueueUri = _messageContextAccessor.MessageContext.QueueUri;

        _logger.Information("Received TestMessage with uniqueId {uniqueId}", messageBase.UniqueId);

        return true;
    }
}
