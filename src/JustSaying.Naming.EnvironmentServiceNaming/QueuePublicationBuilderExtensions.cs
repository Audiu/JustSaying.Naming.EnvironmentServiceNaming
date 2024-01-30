using JustSaying.Fluent;
using JustSaying.Models;

namespace JustSaying.Naming.EnvironmentServiceNaming
{
    public static class QueuePublicationBuilderExtensions
    {
        public static QueuePublicationBuilder<T> ConfigurePointToPointPublisher<T>(
            this QueuePublicationBuilder<T> builder,
            string environment) where T : Message
        {
            builder.WithName(EnvironmentServiceNamingStrategy.GetPointToPointQueueName<T>(environment));

            return builder;
        }
    }
}