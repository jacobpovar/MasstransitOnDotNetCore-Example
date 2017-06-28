
namespace MasstransitOnDotNetCore.Integration
{
    using System;

    using MassTransit;

    internal interface ICachedConfigurator
    {
        void Configure(IReceiveEndpointConfigurator configurator, IServiceProvider services);
    }
}