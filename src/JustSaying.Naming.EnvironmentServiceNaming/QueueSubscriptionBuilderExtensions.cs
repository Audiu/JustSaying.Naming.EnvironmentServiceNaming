using JustSaying.Fluent;
using JustSaying.Models;

namespace JustSaying.Naming.EnvironmentServiceNaming
{
    public static class QueueSubscriptionBuilderExtensions
    {
        public static QueueSubscriptionBuilder<T> ConfigurePointToPointQueue<T>(
            this QueueSubscriptionBuilder<T> builder,
            string environment) where T : Message
        {
            builder.WithQueueName(EnvironmentServiceNamingStrategy.GetPointToPointQueueName<T>(environment));

            return builder;
        }
    }
}
