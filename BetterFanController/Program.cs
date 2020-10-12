using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using PCIIdentificationResolver;
using static System.Globalization.NumberStyles;

namespace BetterFanController
{
    public class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => 
            {
                logging.AddConsole()
                .SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
                services.AddHostedService<FanController>();
            })
            .UseSystemd();
        }
    }
}
