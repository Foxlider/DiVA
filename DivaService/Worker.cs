using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;

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
                Analytics.TrackEvent("[Worker] ExecuteAsync - DiVA Starting", new Dictionary<string, string> {
                    { "Version", DiVA.DiVA.GetVersion() },
                    { "Region", RegionInfo.CurrentRegion.TwoLetterISORegionName},
                    { "CPU Architecture", Environment.Is64BitOperatingSystem ? "x64" : "x86" },
                    { "OS Version", Environment.OSVersion.VersionString },
                    { "Machine Name", Environment.MachineName }
                });
                await Task.Run(() => DiVA_bot.RunAsync(), stoppingToken);
                //STATIC VERSION : await Task.Run(() =>DiVA.DiVA.RunAsync(Array.Empty<string>()), stoppingToken); 

            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Analytics.TrackEvent("[Worker] StopAsync - DiVA Stopping");
            await DiVA_bot.Disconnect();
            await base.StopAsync(cancellationToken);
        }

    }
}
