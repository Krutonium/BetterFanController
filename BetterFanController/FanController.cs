using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BetterFanController
{

    public class FanController : BackgroundService
    {
        private ILogger<FanController> _logger;
        private IList<GPU> _gpus;
        int _longestName = 0;

        public FanController(ILogger<FanController> logger)
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
                        if (_longestName < gpu.DeviceName.Length)
                        {
                            _longestName = gpu.DeviceName.Length;
                        }
                        _gpus.Add(gpu);
                    }
                }
            }
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
                       _logger.LogInformation("Adjusting for Temperature Spike...");
                    }
                    else
                    {
                        gpu.TemperatureHistory[tempCurrentLocation] = gpu.Temperature;
                    }
                    gpu.FanSpeed = FanSpeedCalc(historicValue);
                    string loggableDeviceName = gpu.DeviceName.PadRight(_longestName, ' ');
                    _logger.LogInformation($"Set GPU {loggableDeviceName} at {gpu.Temperature}c (Average temp of {historicValue}c) to a PWM Speed of {gpu.FanSpeed}");
                    
                    //MAKE THIS CONFIGURABLE
                    if (gpu.MaxPower != gpu.TargetPower)
                    {
                        gpu.TargetPower = gpu.MaxPower;
                        _logger.LogInformation("Adjusted GPU Power Targets to Maximum");
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

        private byte FanSpeedCalc(int temperature)
        {
            const int inputRangeMin = 30; //Minimum Temperature
            const int inputRangeMax = 50; //Maximum Temperature
            const int inputRangeSpan = inputRangeMax - inputRangeMin;
            var fanSpeed = Math.Min(Math.Max(byte.MinValue, temperature - inputRangeMin) * byte.MaxValue / inputRangeSpan, byte.MaxValue);
            return (byte) fanSpeed;
            //This calculates how fast to run the fan to try to hold at about halfway between the two values - essentially your target temperature.
            //That being said it's possible to go well above and below those values by simply running the GPU or not.
        }
    }
}