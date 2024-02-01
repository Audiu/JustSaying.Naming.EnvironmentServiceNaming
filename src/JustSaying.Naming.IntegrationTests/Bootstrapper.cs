﻿using System;
using System.Threading;
using System.Threading.Tasks;
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

namespace JustSaying.Naming.IntegrationTests;

[SetUpFixture]
public class Bootstrapper
{
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
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to bootstrap");
            TestContext.Progress.WriteLine($"Failed to bootstrap {ex.Message}");

            throw;
        }
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        Container?.Dispose();
        LoggerFactory?.Dispose();
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

        var builder = container.AddJustSayingReturnBuilder(
            new AwsConfig(null, null, "eu-west-1", "http://localhost.localstack.cloud:4566"),
            namingStrategy,
            namingStrategy,
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
