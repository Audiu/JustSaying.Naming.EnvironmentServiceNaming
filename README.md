# EnvironmentServiceNaming

This is a page for [JustSaying](https://github.com/justeattakeaway/JustSaying) v7 to add a naming convention based on environment & service name. 
It also has an extra method for defining point to point queues.

### What it looks like

Environment and service name are converted to lower case and any non-alphanumeric characters are removed.

Topic queues:

`{environment}-{service}-{messageType}`

Topic name:

`{environment}-{messageType}`

Point to point queues:

`{environment}-p2p-{messageType}`

# How to use

### Set up default naming conventions

This will set up the default naming conventions for all queues and topics.

```csharp
var environment = "test";
var serviceName = "my-service";

var namingStrategy = new EnvironmentNamingStrategy(environment, serviceName);

container.RegisterInstance<IQueueNamingConvention>(queueNamingConvention);
container.RegisterInstance<ITopicNamingConvention>(topicNamingConvention);

var builder = new MessagingBusBuilder()
    // Other configuration here
    .Messaging(
        x =>
        {
            // Other configuration here
            x.WithQueueNamingConvention(queueNamingConvention);
            x.WithTopicNamingConvention(topicNamingConvention);
        });
```

To use the point to point naming convention, the subscription / publisher needs to be setup during configuration:

```csharp
    builder.Subscriptions(
        x =>
        {
           
            x.ForQueue<TestMessagePointToPoint>(
                cfg =>
                {
                    cfg.ConfigurePointToPointQueue(Environment);
                });
        }
    );
    
     builder.Publications(
        x =>
        {
            x.WithQueue<TestMessagePointToPoint>(cfg => cfg.ConfigurePointToPointPublisher(Environment));
        });
```

If individual p2p queues aren't configured as above, then it will default to:

`{environment}-{service}-{messageType}`

## Additional Notes

There are helper extension methods for configuration which are used above:

`ConfigurePointToPointPublisher`
`ConfigurePointToPointQueue`

In a similar vein exists `ConfigureTopic` which provides a similar interface for configuring subscriptions, but is not strictly needed for configuring naming conventions.