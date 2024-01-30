using System;
using JustSaying.Fluent;
using JustSaying.Models;

namespace JustSaying.Naming.EnvironmentServiceNaming
{
    public static class TopicSubscriptionBuilderExtensions
    {
        public static TopicSubscriptionBuilder<T> ConfigureTopic<T>(
            this TopicSubscriptionBuilder<T> builder,
            TimeSpan? visibilityTimeout = null,
            TimeSpan? messageRetention = null,
            TimeSpan? deliveryDelay = null,
            int? retriesBeforeErrorQueue = null) where T : Message
        {
            builder
                .WithReadConfiguration(
                    c =>
                    {
                        if (messageRetention.HasValue)
                        {
                            c.MessageRetention = (TimeSpan)messageRetention;   
                        }

                        if (visibilityTimeout.HasValue)
                        {
                            c.VisibilityTimeout = (TimeSpan)visibilityTimeout;   
                        }

                        if (deliveryDelay.HasValue)
                        {
                            c.DeliveryDelay = (TimeSpan)deliveryDelay;   
                        }
                        
                        if (retriesBeforeErrorQueue.HasValue)
                        {
                            c.RetryCountBeforeSendingToErrorQueue = (int)retriesBeforeErrorQueue;   
                        }
                    });

            return builder;
        }
    }
}