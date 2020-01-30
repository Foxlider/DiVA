using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace DivaService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private DiVA.DiVA DiVA_bot;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                //DiVA_bot = new DiVA.DiVA(Array.Empty<string>());
                DiVA_bot = new DiVA.DiVA(Array.Empty<string>());
                await Task.Run(() => DiVA_bot.RunAsync(), stoppingToken);
                //STATIC VERSION : await Task.Run(() =>DiVA.DiVA.RunAsync(Array.Empty<string>()), stoppingToken); 

            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await DiVA_bot.Disconnect();
            await base.StopAsync(cancellationToken);
        }

    }
}
