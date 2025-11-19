using System;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using JustSaying.Extensions.DependencyInjection.SimpleInjector;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.Naming.EnvironmentServiceNaming;
using JustSaying.Naming.IntegrationTests.NamingTest.Messages;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Testcontainers.LocalStack;

namespace JustSaying.Naming.IntegrationTests;

[SetUpFixture]
public class Bootstrapper
{
    private static LocalStackContainer _localStackContainer;
    private static string _serviceUrl;

    public static ILoggerFactory LoggerFactory { get; private set; }

    public static Container Container { get; private set; }

    public const string Environment = "integration";
    public const string ServiceName = "justsayingintegrationtests";

    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        LoggerFactory = new LoggerFactory();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        LoggerFactory.AddSerilog(logger);

        Log.Logger = logger;

        logger.Information("Configured logging");
        TestContext.Progress.WriteLine("Configured logging");

        try
        {
            // Set dummy AWS credentials to allow Container.Verify() to succeed
            // These won't be used because LocalStack is configured with anonymous credentials
            var originalAccessKey = System.Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var originalSecretKey = System.Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

            try
            {
                System.Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
                System.Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");

                // Start LocalStack container
                logger.Information("Starting LocalStack container...");
                TestContext.Progress.WriteLine("Starting LocalStack container...");

                _localStackContainer = new LocalStackBuilder()
                    .WithImage("localstack/localstack:latest")
                    .Build();

                await _localStackContainer.StartAsync();

                _serviceUrl = _localStackContainer.GetConnectionString();

                logger.Information($"LocalStack started at: {_serviceUrl}");
                TestContext.Progress.WriteLine($"LocalStack started at: {_serviceUrl}");

                Container = new Container();
                ConfigureInjection(Container);

                Container.Verify();

                logger.Information("Configured and verified runtime injection");
                TestContext.Progress.WriteLine("Configured and verified runtime injection");

                // Boot listener
                var publisher = Container.GetInstance<IMessagePublisher>();
                await publisher.StartAsync(CancellationToken.None);

                var messagingBus = Container.GetInstance<IMessagingBus>();
                await messagingBus.StartAsync(CancellationToken.None);
            }
            finally
            {
                // Restore original values
                System.Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", originalAccessKey);
                System.Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", originalSecretKey);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to bootstrap");
            TestContext.Progress.WriteLine($"Failed to bootstrap {ex.Message}");

            throw;
        }
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        Container?.Dispose();
        LoggerFactory?.Dispose();

        if (_localStackContainer != null)
        {
            await _localStackContainer.DisposeAsync();
        }
    }

    private static void ConfigureInjection(Container container)
    {
        container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

        container.RegisterInstance(Log.Logger);

        ConfigureJustSaying(container);
    }

    private static void ConfigureJustSaying(Container container)
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog(Log.Logger);

        container.RegisterInstance<ILoggerFactory>(loggerFactory);

        var namingStrategy = new EnvironmentServiceNamingStrategy(Environment, ServiceName);

        container.AddJustSayingNoOpMessageMonitor();

        var builder = container.AddJustSayingReturnBuilder(
            new MessagingConfig
            {
                Region = System.Environment.GetEnvironmentVariable("AWS_REGION"),
                QueueNamingConvention = namingStrategy,
                TopicNamingConvention = namingStrategy,
            },
            _serviceUrl,
            builder =>
            {
                builder.Subscriptions(
                    x =>
                    {
                        x.ForTopic<TestMessage>(
                            cfg =>
                            {
                                cfg.WithMiddlewareConfiguration(m =>
                                {
                                    m.UseSimpleInjectorScope();
                                    m.UseDefaults<TestMessage>(typeof(TestMessageHandler)); // Add default middleware pipeline
                                });
                            });

                        x.ForQueue<TestMessagePointToPoint>(
                            cfg =>
                            {
                                cfg.ConfigurePointToPointQueue(Environment);
                                cfg.WithMiddlewareConfiguration(m =>
                                {
                                    m.UseSimpleInjectorScope();
                                    m.UseDefaults<TestMessagePointToPoint>(typeof(TestMessagePointToPointHandler)); // Add default middleware pipeline
                                });
                            });
                    }
                );

                builder.Publications(
                    x =>
                    {
                        x.WithTopic<TestMessage>();
                        x.WithQueue<TestMessagePointToPoint>(cfg => cfg.ConfigurePointToPointPublisher(Environment));
                    });
            });

        container.Register<IHandlerAsync<TestMessage>, TestMessageHandler>(Lifestyle.Scoped);
        container.Register<IHandlerAsync<TestMessagePointToPoint>, TestMessagePointToPointHandler>(Lifestyle.Scoped);

        // Final steps (we might want to override our publishers/subscribers)
        container.RegisterSingleton(() => builder.BuildPublisher());
        container.RegisterSingleton(() => builder.BuildSubscribers());
    }
}
