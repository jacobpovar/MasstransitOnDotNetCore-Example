namespace MasstransitOnDotNetCore.Integration
{
    using System;
    using System.Collections.Concurrent;

    using MassTransit;

    public static class ConsumerConfiguratorCache
    {
        static CachedConfigurator GetOrAdd(Type type)
        {
            return Cached.Instance.GetOrAdd(type, _ =>
                (CachedConfigurator)Activator.CreateInstance(typeof(CachedConfigurator<>).MakeGenericType(type)));
        }

        public static void Configure(Type consumerType, IReceiveEndpointConfigurator configurator, IServiceProvider container)
        {
            GetOrAdd(consumerType).Configure(configurator, container);
        }

        public static void Cache(Type consumerType)
        {
            GetOrAdd(consumerType);
        }

        interface CachedConfigurator
        {
            void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services);
        }

        class CachedConfigurator<T> : CachedConfigurator
            where T : class, IConsumer
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new ExtensionsDependencyInjectionConsumerFactory<T>(services));
            }
        }

        static class Cached
        {
            internal static readonly ConcurrentDictionary<Type, CachedConfigurator> Instance =
                new ConcurrentDictionary<Type, CachedConfigurator>();
        }
    }
}