using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JustSaying.Naming.EnvironmentServiceNaming
{
    public class EnvironmentServiceNamingStrategy : ITopicNamingConvention, IQueueNamingConvention
    {
        private const int MaxTopicNameLength = 256;
        private const int MaxQueueNameLength = 80;

        private static readonly HashSet<Type> TypesToMapAutomatically = new HashSet<Type>
            {
                typeof(string),
                typeof(object),
                typeof(bool),
                typeof(byte),
                typeof(char),
                typeof(decimal),
                typeof(double),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(sbyte),
                typeof(float),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(void),
                typeof(TimeSpan),
                typeof(DateTime),
                typeof(DateTimeOffset)
            };

        private readonly string _environment;
        private readonly string _service;

        public EnvironmentServiceNamingStrategy(string environment, string service)
        {
            _environment = GetSafeName(environment);
            _service = GetSafeName(service);
        }

        private static string GetSafeName(string name)
        {
            return name
                .Replace(" ", "")
                .Replace(".", "")
                .Replace("-", "")
                .ToLowerInvariant();
        }

        public string QueueName<T>()
        {
            return $"{_environment}-{_service}-{CreateResourceName(typeof(T), MaxQueueNameLength)}".ToLowerInvariant();
        }

        public string TopicName<T>()
        {
            return $"{_environment}-{CreateResourceName(typeof(T), MaxTopicNameLength)}".ToLowerInvariant();
        }

        public string GetPointToPointQueueName<T>()
        {
            return $"{_environment}-p2p-{CreateResourceName(typeof(T), MaxQueueNameLength)}".ToLowerInvariant();
        }

        public static string GetPointToPointQueueName<T>(string environment)
        {
            return $"{environment}-p2p-{CreateResourceName(typeof(T), MaxQueueNameLength)}".ToLowerInvariant();
        }

        private static string CreateResourceName(Type type, int maximumLength)
        {
            var name = Regex.Replace(GetTypeFriendlyName(type), "[^a-zA-Z0-9_-]", string.Empty);

            return name.Length <= maximumLength ? name.ToLowerInvariant() : name.Substring(0, maximumLength);
        }

        private static string GetTypeFriendlyName(Type type)
        {
            var friendlyName = type.Name.ToLowerInvariant();

            if (TypesToMapAutomatically.Contains(type))
            {
                return friendlyName;
            }

            if (type.GetTypeInfo().IsGenericType)
            {
                var indexOfBacktick = friendlyName.IndexOf('`');

                if (indexOfBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(indexOfBacktick);
                }

                friendlyName += string.Join("_", type.GenericTypeArguments.Select(GetTypeFriendlyName));
            }

            if (type.IsArray)
            {
                return GetTypeFriendlyName(type.GetElementType()) + "_";
            }

            return friendlyName;
        }
    }
}