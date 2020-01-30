using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace DivaService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            { CreateHostBuilder(args).Build().Run(); }
            catch(OperationCanceledException)
            { /* The App was just shut down */ }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
    }
}
