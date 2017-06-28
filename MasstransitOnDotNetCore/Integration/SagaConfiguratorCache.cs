namespace MasstransitOnDotNetCore.Integration
{
    using System;
    using System.Collections.Concurrent;

    using MassTransit;
    using MassTransit.Saga;

    using Microsoft.Extensions.DependencyInjection;

    public static class SagaConfiguratorCache
    {
        static CachedConfigurator GetOrAdd(Type type)
        {
            return Cached.Instance.GetOrAdd(type, _ =>
                (CachedConfigurator)Activator.CreateInstance(typeof(CachedConfigurator<>).MakeGenericType(type)));
        }

        public static void Configure(Type sagaType, IReceiveEndpointConfigurator configurator, IServiceProvider services)
        {
            GetOrAdd(sagaType).Configure(configurator, services);
        }

        public static void Cache(Type sagaType)
        {
            GetOrAdd(sagaType);
        }

        static class Cached
        {
            internal static readonly ConcurrentDictionary<Type, CachedConfigurator> Instance =
                new ConcurrentDictionary<Type, CachedConfigurator>();
        }

        interface CachedConfigurator
        {
            void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services);
        }

        class CachedConfigurator<T> : CachedConfigurator
            where T : class, ISaga
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                var sagaRepository = services.GetService<ISagaRepository<T>>();

                var repository = new ExtensionsDependencyInjectionSagaRepository<T>(sagaRepository, services);

                configurator.Saga(repository);
            }
        }
    }
}