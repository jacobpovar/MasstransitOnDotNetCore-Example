namespace MasstransitOnDotNetCore
{
    using System;
    using System.Threading.Tasks;

    using MasstransitOnDotNetCore.Integration;

    using MassTransit;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            // Add framework services.
            services.AddMvc();

            services.Configure<RabbitMqConfiguration>(this.Configuration.GetSection("rabbitmq.settings"));

            // consumer setup
            services.AddScoped<RequestConsumer>();

            // bus setup
            services.AddSingleton<IBusControl>(provider =>
                {
                    string rabbitMqHost = provider.GetService<IOptions<RabbitMqConfiguration>>().Value.Host;
                    string username = provider.GetService<IOptions<RabbitMqConfiguration>>().Value.Username;
                    string password = provider.GetService<IOptions<RabbitMqConfiguration>>().Value.Password;

                    var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
                        {
                            var host = cfg.Host(new Uri(rabbitMqHost), h =>
                                {
                                    h.Username(username);
                                    h.Password(password);
                                });

                            cfg.ReceiveEndpoint(host, "request_service", e =>
                                {
                                    if (e != null)
                                    {
                                        e.LoadFrom(services, provider);
                                    }
                                });
                        });

                    busControl.Start();
                    return busControl;
                });

            services.AddSingleton<IBus>(provider => provider.GetService<IBusControl>());

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
        }
    }

    public class RabbitMqConfiguration
    {
        public string Host { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }

    public class RequestConsumer : IConsumer<ISimpleRequest>
    {
        private ILogger<RequestConsumer> _logger;

        public RequestConsumer(ILogger<RequestConsumer> logger)
        {
            this._logger = logger;
        }

        public async Task Consume(ConsumeContext<ISimpleRequest> context)
        {
            this._logger.LogDebug($"New Message received: {context.Message}");

            await Task.CompletedTask;
        }
    }

    public interface ISimpleRequest
    {
    }

    public class SimpleRequest : ISimpleRequest
    {
    }
}
