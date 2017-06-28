namespace MasstransitOnDotNetCore.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using GreenPipes.Internals.Extensions;

    using MassTransit;
    using MassTransit.ConsumeConfigurators;
    using MassTransit.Saga;
    using MassTransit.Saga.SubscriptionConfigurators;

    using Microsoft.Extensions.DependencyInjection;

    public static class ExtensionsDependencyInjectionIntegrationExtensions
    {
        public static void LoadFrom(this IReceiveEndpointConfigurator configurator,
            IServiceCollection serviceCollection, IServiceProvider services)
        {
            IList<Type> concreteTypes = FindTypes<IConsumer>(serviceCollection, x => !x.HasInterface<ISaga>());
            if (concreteTypes.Count > 0)
            {
                foreach (Type concreteType in concreteTypes)
                    ConsumerConfiguratorCache.Configure(concreteType, configurator, services);
            }

            IList<Type> sagaTypes = FindTypes<ISaga>(serviceCollection, x => true);
            if (sagaTypes.Count > 0)
            {
                foreach (Type sagaType in sagaTypes)
                    SagaConfiguratorCache.Configure(sagaType, configurator, services);
            }
        }

        public static void Consumer<T>(this IReceiveEndpointConfigurator configurator, IServiceProvider services, Action<IConsumerConfigurator<T>> configure = null)
            where T : class, IConsumer
        {
            var factory = new ExtensionsDependencyInjectionConsumerFactory<T>(services);
            configurator.Consumer(factory, configure);
        }

        public static void Saga<T>(this IReceiveEndpointConfigurator configurator, IServiceProvider services, Action<ISagaConfigurator<T>> configure = null)
            where T : class, ISaga
        {
            var sagaRepository = services.GetRequiredService<ISagaRepository<T>>();

            var extensionsSagaRepository = new ExtensionsDependencyInjectionSagaRepository<T>(sagaRepository, services);

            configurator.Saga(extensionsSagaRepository, configure);
        }

        public static void AddMassTransit(this IServiceCollection serviceCollection, Action<MassTransitOptions> opt = null)
        {
            var options = new MassTransitOptions(serviceCollection);
            serviceCollection.AddSingleton(options);

            if (opt != null)
            {
                opt(options);
            }
        }

        public static void AddMassTransit(this IServiceCollection services, params Assembly[] assemblies)
        {
            AddRequiredServices(services);
            AddHandlers(services, assemblies);
        }

        public static void AddMassTransit(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            AddRequiredServices(services);
            AddHandlers(services, assemblies);
        }

        public static void AddMassTransit(this IServiceCollection services, params Type[] handlerAssemblyMarkerTypes)
        {
            AddRequiredServices(services);
            AddHandlers(services, handlerAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly));
        }

        public static void AddMassTransit(this IServiceCollection services, IEnumerable<Type> handlerAssemblyMarkerTypes)
        {
            AddRequiredServices(services);
            AddHandlers(services, handlerAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly));
        }

        private static void AddRequiredServices(IServiceCollection services)
        {

        }

        private static void AddHandlers(this IServiceCollection services, IEnumerable<Assembly> assembliesToScan)
        {
            assembliesToScan = assembliesToScan as Assembly[] ?? assembliesToScan.ToArray();

            foreach(var type in assembliesToScan.SelectMany(a => a.ExportedTypes))
            {
                if (type.CanBeCastTo(typeof(ISaga)))
                {
                    services.AddScoped(type);
                    SagaConfiguratorCache.Cache(type);
                }
                else if (type.CanBeCastTo(typeof(IConsumer)))
                {
                    services.AddScoped(type);
                    ConsumerConfiguratorCache.Cache(type);
                }
            }
        }

        private static bool CanBeCastTo(this Type handlerType, Type interfaceType)
        {
            if (handlerType == null)
            {
                return false;
            }

            if (handlerType == interfaceType)
            {
                return true;
            }

            return interfaceType.GetTypeInfo().IsAssignableFrom(handlerType.GetTypeInfo());
        }

        private static IList<Type> FindTypes<T>(IServiceCollection container, Func<Type, bool> filter)
        {
            return
                container
                    .Where(r => r.ImplementationType != null && r.ImplementationType.HasInterface<T>())
                    .Select(x => x.ImplementationType)
                    .Where(filter)
                    .ToList();
        }
    }
}