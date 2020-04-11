using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.Util;
using Microsoft.Extensions.DependencyInjection;

namespace MassTransitTest
{
    static class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = ConfigureServiceProvider();

            var busControl = serviceProvider.GetRequiredService<IBusControl>();

            try
            {
                busControl.Start();

                TaskUtil.Await(() => Submit(serviceProvider));

                Console.Read();
            }
            finally
            {
                busControl.Stop();
            }
        }

        private static async Task Submit(IServiceProvider provider)
        {
            var bus = provider.GetRequiredService<IBus>();
            var sendEndpoint = await bus.GetSendEndpoint(new Uri("loopback://localhost/test")).ConfigureAwait(false);
            await sendEndpoint.Send<TheMessage>(new { Data = "Hello World" }).ConfigureAwait(false);
        }

        private static IServiceProvider ConfigureServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddMassTransit(cfg =>
            {
                cfg.AddConsumer<TheMessageConsumer>();

                cfg.AddBus(BusFactory);
            });

            services.AddScoped<ConsumingService>();

            // Temporary fix: Add the scoped registration again
            // services.AddScoped(provider => (ISendEndpointProvider)provider.GetService<ScopedConsumeContextProvider>()?.GetContext() ??
            //     provider.GetRequiredService<IBus>());

            return services.BuildServiceProvider();
        }

        private static IBusControl BusFactory(IServiceProvider provider)
        {
            return Bus.Factory.CreateUsingInMemory(cfg =>
            {
                cfg.ReceiveEndpoint("test", endpoint =>
                {
                    endpoint.ConfigureConsumers(provider);
                });
            });
        }
    }

    public interface TheMessage
    {
        string Data { get; }
    }

    class TheMessageConsumer : IConsumer<TheMessage>
    {
        private readonly ConsumingService _consumingService;

        public TheMessageConsumer(ConsumingService consumingService)
        {
            _consumingService = consumingService;
        }

        public Task Consume(ConsumeContext<TheMessage> context)
        {
            return Task.CompletedTask;
        }
    }

    class ConsumingService
    {
        public ConsumingService(ISendEndpointProvider sendEndpointProvider)
        {
            // sendEndpointProvider should be an instance of ConsumeContext<TheMessage>
            Console.WriteLine(sendEndpointProvider.GetType());
        }
    }
}
