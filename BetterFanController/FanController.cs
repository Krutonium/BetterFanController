using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BetterFanController
{

    public class FanController : BackgroundService
    {
        private ILogger<FanController> _logger;
        private IList<GPU> _gpus;
        private Configuration _configuration;
        
        public FanController(ILogger<FanController> logger, IConfigurationProvider configurationProvider)
        {
            _logger = logger;
            _gpus = new List<GPU>();
            
            foreach (var d in Directory.GetDirectories("/sys/class/drm/"))
            {
                var t = Path.GetFileName(d);
                if (t.StartsWith("card") && char.IsDigit(t[^1]) && t.Length == 5) //Has to be a card, end in a digit, and be 5 characters long.
                {
                    var gpu = GPU.LoadFromPath(d);

                    if (gpu.VendorId != 4098)
                    {
                        //This is an nVidia or Intel Card, and it's probably not going to work.
                        //So we're not going to add it.
                    }
                    else
                    {
                        _logger.LogInformation($"Found {gpu.VendorName} {gpu.DeviceName} at path {d}.");

                        _gpus.Add(gpu);
                        bool contains = configurationProvider.Configuration.GpuConfigInfo.Any(p => p.DeviceID == gpu.DeviceId);
                        if (!contains)
                        {
                            GpuConfigs _tmpConfig = new GpuConfigs();
                            _tmpConfig.NameOverride = gpu.DeviceName;
                            _tmpConfig.TargetPower = gpu.TargetPower;
                            _tmpConfig.DeviceID = gpu.DeviceId;
                            _tmpConfig.MaxPowerDoNotChange = gpu.MaxPower;
                            _tmpConfig.TargetPower = gpu.TargetPower;
                            configurationProvider.Configuration.GpuConfigInfo.Add(_tmpConfig);
                            configurationProvider.SaveConfiguration();
                        }
                    }
                }
            }

            _logger.LogInformation("Indexed GPUs");
            _configuration = configurationProvider.Configuration;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Please note that the first 5 lines of this log are wrong while the application calibrates itself.");
            
            //We now have all of our GPU's in a list of GPU's.
            int tempCurrentLocation = 0; //Keeps track of which value in the history to update this loop.
            
            while (!stoppingToken.IsCancellationRequested)
            {
                if (tempCurrentLocation == 5)
                {
                    tempCurrentLocation = 0;
                }

                foreach (var gpu in _gpus)
                {
                    int configIndex = _configuration.GpuConfigInfo.FindIndex(c => c.DeviceID == gpu.DeviceId);
                    if (gpu.FanState != FanStates.Manual)
                    {
                        gpu.FanState = FanStates.Manual;
                        //Other Options: Automatic and Maximum.
                        //On some GPU's, setting it to Maximum will cause it to run them for a couple seconds
                        //then default back to Automatic, but without it being functional. Ask me how I know.
                    }

                    int historicValue = gpu.TemperatureHistory.Sum() / 5; //Gets Average Temp over the last 5 seconds
                    if (gpu.Temperature - historicValue > 10) //If the difference between average and current is more than 10 degrees
                    {
                        //Temperature is rising too quickly!!
                        Array.Fill(gpu.TemperatureHistory, gpu.Temperature);
                       _logger.LogInformation("Adjusting for Temperature Spike on GPU " + _configuration.GpuConfigInfo[configIndex].NameOverride);
                    }
                    else
                    {
                        gpu.TemperatureHistory[tempCurrentLocation] = gpu.Temperature;
                    }
                    gpu.FanSpeed = FanSpeedCalc(historicValue, _configuration.GpuConfigInfo[configIndex].MinimumTemperature, 
                        _configuration.GpuConfigInfo[configIndex].MaxTemperature);
                    
                    string loggableDeviceName = _configuration.GpuConfigInfo[configIndex].NameOverride
                        .PadRight(_configuration.LongestName, ' ');
                    _logger.LogInformation($"Set GPU {loggableDeviceName} at {gpu.Temperature}c (Average temp of {historicValue}c) to a PWM Speed of {gpu.FanSpeed}");
                    
                    //MAKE THIS CONFIGURABLE
                    if (gpu.TargetPower != _configuration.GpuConfigInfo[configIndex].TargetPower)
                    {
                        gpu.TargetPower = _configuration.GpuConfigInfo[configIndex].TargetPower;
                        _logger.LogInformation("Adjusted GPU Power Target on GPU " + _configuration.GpuConfigInfo[configIndex].NameOverride);
                    }
                }

                await Task.Delay(1000);
                tempCurrentLocation += 1;
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //On shutdown set the GPUs back to Automatic.
            _logger.LogInformation($"Shutting down. Setting GPUs to Automatic.");
            foreach (var gpu in _gpus)
            {
                gpu.FanState = FanStates.Automatic;
            }

            return base.StopAsync(cancellationToken);
        }

        private static byte FanSpeedCalc(int temperature, int minTemp = 30, int maxTemp = 50)
        {
            int inputRangeMin = minTemp; //Minimum Temperature
            int inputRangeMax = maxTemp; //Maximum Temperature
            int inputRangeSpan = inputRangeMax - inputRangeMin;
            var fanSpeed = Math.Min(Math.Max(byte.MinValue, temperature - inputRangeMin) * byte.MaxValue / inputRangeSpan, byte.MaxValue);
            return (byte) fanSpeed;
            //This calculates how fast to run the fan to try to hold at about halfway between the two values - essentially your target temperature.
            //That being said it's possible to go well above and below those values by simply running the GPU or not.
            //Reason this is Static: We don't need more than 1, really.
        }
        
    }
}