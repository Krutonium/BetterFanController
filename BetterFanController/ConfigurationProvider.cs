using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BetterFanController
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public Configuration Configuration { get; }

        private ILogger<Configuration> _logger;

        public ConfigurationProvider(ILogger<Configuration> logger)
        {
            _logger = logger;

            if (File.Exists("/etc/BetterFanController.json"))
            {
                Configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("/etc/BetterFanController.json"));
            }
            else
            {
                // Create a new config file
                Configuration = new Configuration();
            }
        }

        public void SaveConfiguration()
        {
            File.WriteAllText("/etc/BetterFanController.json",JsonConvert.SerializeObject(Configuration, Formatting.Indented));
            _logger.LogInformation("Saved Config to /etc/BetterFanController.json");
        }
    }
}