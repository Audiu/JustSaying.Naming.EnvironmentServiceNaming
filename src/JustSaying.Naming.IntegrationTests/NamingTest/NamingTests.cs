using System;
using System.Threading.Tasks;
using FluentAssertions;
using JustSaying.Messaging;
using JustSaying.Naming.IntegrationTests.NamingTest.Messages;
using NUnit.Framework;

namespace JustSaying.Naming.IntegrationTests.NamingTest;

[TestFixture]
public class NamingTests
{
    [Test]
    public async Task TestTopic()
    {
        var id = Guid.NewGuid().ToString();

        var messagePublisher = Bootstrapper.Container.GetInstance<IMessagePublisher>();

        await messagePublisher.PublishAsync(new TestMessage(id));

        await Task.Delay(2000);

        TestMessageHandler.LastUniqueId.Should().Be(id);
        TestMessageHandler.LastQueueUri.ToString().Should()
            .Contain($"{Bootstrapper.Environment}-{Bootstrapper.ServiceName}-testmessage");
    }

    [Test]
    public async Task TestQueue()
    {
        var id = Guid.NewGuid().ToString();

        var messagePublisher = Bootstrapper.Container.GetInstance<IMessagePublisher>();

        await messagePublisher.PublishAsync(new TestMessagePointToPoint(id));

        await Task.Delay(2000);

        TestMessagePointToPointHandler.LastUniqueId.Should().Be(id);
        TestMessagePointToPointHandler.LastQueueUri.ToString().Should()
            .Contain($"{Bootstrapper.Environment}-p2p-testmessagepointtopoint");
    }
}
