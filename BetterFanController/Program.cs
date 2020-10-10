using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PCIIdentificationResolver;
using static System.Globalization.NumberStyles;

namespace BetterFanController
{
    class Program
    {
        static void Main(string[] args)
        {
            List<GPU> Gpus = new List<GPU>();
            int LongestName = 0;
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
                        Console.WriteLine($"Found {gpu.VendorName} {gpu.DeviceName} at path {d}.");
                        if (LongestName < gpu.DeviceName.Length)
                        {
                            LongestName = gpu.DeviceName.Length;
                        }
                        Gpus.Add(gpu);
                    }
                }
            }
            Console.WriteLine("Please note that the first 5 lines of this log are wrong while the application calibrates itself.");
            //We now have all of our GPU's in a list of GPU's.
            int tempCurrentLocation = 0; //Keeps track of which value in the history to update this loop.
            while (true)
            {
                if (tempCurrentLocation == 5)
                {
                    tempCurrentLocation = 0;
                }

                foreach (var gpu in Gpus)
                {
                    if (gpu.FanState != FanStates.Manual)
                    {
                        gpu.FanState = FanStates.Manual;
                        //Other Options: Automatic and Maximum.
                        //On some GPU's, setting it to Maximum will cause it to run them for a couple seconds
                        //then default back to Automatic, but without it being functional. Ask me how I know.

                        //We should add code so that on shutdown it sets the GPU's back to Automatic.
                    }

                    int HistoricValue = gpu.TemperatureHistory.Sum() / 5; //Gets Average Temp over the last 5 seconds
                    if (gpu.Temperature - HistoricValue > 10) //If the difference between average and current is more than 10 degrees
                    {
                        //Temperature is rising too quickly!!
                        Array.Fill(gpu.TemperatureHistory, gpu.Temperature);
                        Console.WriteLine("Adjusting for Temperature Spike...");
                    }
                    else
                    {
                        gpu.TemperatureHistory[tempCurrentLocation] = gpu.Temperature;
                    }
                    gpu.FanSpeed = FanSpeedCalc(HistoricValue);
                    string LoggableDeviceName = gpu.DeviceName.PadRight(LongestName, ' ');
                    Console.WriteLine("Set GPU {0} at {1}c (Average temp of {2}c) to a PWM Speed of {3}", 
                        LoggableDeviceName, gpu.Temperature, HistoricValue, gpu.FanSpeed);
                    
                    //MAKE THIS CONFIGURABLE
                    if (gpu.MaxPower != gpu.TargetPower)
                    {
                        gpu.TargetPower = gpu.MaxPower;
                        Console.WriteLine("Adjusted GPU Power Targets to Maximum");
                    }
                }

                System.Threading.Thread.Sleep(1000);
                Console.Clear();
                tempCurrentLocation += 1;
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static byte FanSpeedCalc(int temperature)
        {
            const int inputRangeMin = 30; //Minimum Temperature
            const int inputRangeMax = 50; //Maximum Temperature
            const int inputRangeSpan = inputRangeMax - inputRangeMin;
            var fanSpeed = Math.Min(Math.Max(byte.MinValue, temperature - inputRangeMin) * byte.MaxValue / inputRangeSpan, byte.MaxValue);
            return (byte) fanSpeed;
            //This calculates how fast to run the fan to try to hold at about halfway between the two values - essentially your target temperature.
            //That being said it's possible to go well above and below those values by simply running the GPU or not.
            //Reason this is Static: We don't need more than 1, really.
        }

    }
}
