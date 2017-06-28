namespace MasstransitOnDotNetCore.Integration
{
    using System;
    using System.Threading.Tasks;

    using GreenPipes;

    using MassTransit;
    using MassTransit.Util;

    using Microsoft.Extensions.DependencyInjection;

    public class ExtensionsDependencyInjectionConsumerFactory<TConsumer> : IConsumerFactory<TConsumer>
        where TConsumer : class
    {
        readonly IServiceProvider _services;

        public ExtensionsDependencyInjectionConsumerFactory(IServiceProvider services)
        {
            this._services = services;
        }

        public async Task Send<T>(ConsumeContext<T> context, IPipe<ConsumerConsumeContext<TConsumer, T>> next) where T : class
        {
            using (var scope = this._services.CreateScope())
            {
                var consumer = scope.ServiceProvider.GetService<TConsumer>();
                if (consumer == null)
                {
                    throw new ConsumerException($"Unable to resolve consumer type '{TypeMetadataCache<TConsumer>.ShortName}'.");
                }

                var consumerConsumeContext = context.PushConsumer(consumer);

                await next.Send(consumerConsumeContext).ConfigureAwait(false);
            }
        }

        public void Probe(ProbeContext context)
        {
            context.CreateConsumerFactoryScope<TConsumer>("msedi");
        }
    }
}