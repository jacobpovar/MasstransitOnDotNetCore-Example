namespace MasstransitOnDotNetCore.Integration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using MassTransit;

    using Microsoft.Extensions.DependencyInjection;

    public class MassTransitOptions
    {
        private readonly IServiceCollection _services;
        private readonly ConcurrentDictionary<Type, ICachedConfigurator> _consumerHandlers =
                new ConcurrentDictionary<Type, ICachedConfigurator>();
        public MassTransitOptions(IServiceCollection services)
        {
            this._services = services;
        }

        public void AddConsumer<T>()
            where T : class, IConsumer
        {
            this._services.AddScoped<T>();

            this._consumerHandlers.GetOrAdd(typeof(T), _ => new CachedConfigurator<T>());
        }

        internal IEnumerable<ICachedConfigurator> GetConsumerHandlers()
        {
            return this._consumerHandlers.Values.ToList();
        }

        class CachedConfigurator<T> : ICachedConfigurator
            where T : class, IConsumer
        {
            public void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services)
            {
                configurator.Consumer(new ExtensionsDependencyInjectionConsumerFactory<T>(services));
            }
        }
    }
}