using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace DivaService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            { AppCenter.Start("4ad1e3b3-7ef0-4959-a32c-af5aca211fb1", typeof(Analytics), typeof(Crashes)); }
            catch 
            { /* AppCenter could not be started */ }

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
