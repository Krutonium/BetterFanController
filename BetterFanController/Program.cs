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
            foreach (var d in Directory.GetDirectories("/sys/class/drm/"))
            {
                var t = Path.GetFileName(d);
                if (t.StartsWith("card") && char.IsDigit(t[^1]) && t.Length == 5
                ) //Has to be a card, end in a digit, and be 5 characters long.
                {

                    //d contains the card that we now know about.
                    GPU p = new GPU();
                    p.path = d;
                    var vendor = File.ReadAllText(p.path + "/device/vendor");
                    var device = File.ReadAllText(p.path + "/device/device");
                    vendor = vendor.Substring(2, vendor.Length - 2);
                    device = device.Substring(2, vendor.Length - 2);
                    p.VendorID = ushort.Parse(vendor, HexNumber);
                    p.DeviceID = ushort.Parse(device, HexNumber);
                    if (p.VendorID != 4098)
                    {
                        //This is an nVidia or Intel Card, and it's probably not going to work.
                        //So we're not going to add it.
                    }
                    else
                    {
                        p.gpuNumber = int.Parse(t.Substring(t.Length - 1, 1));
                        //Until I can find a way to get the actual name of the GPU, this will have to do.
                        p.gpuName = p.gpuNumber.ToString();
                        Gpus.Add(p);
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

                    int HistoricValue = gpu.temperatureHistory.Sum() / 5; //Gets Average Temp over the last 5 seconds
                    if (gpu.Temperature - HistoricValue > 10) //If the difference between average and current is more than 10 degrees
                    {
                        //Temperature is rising too quickly!!
                        Array.Fill(gpu.temperatureHistory, gpu.Temperature);
                        Console.WriteLine("Adjusting for Temperature Spike...");
                    }
                    else
                    {
                        gpu.temperatureHistory[tempCurrentLocation] = gpu.Temperature;
                    }
                    gpu.FanSpeed = FanSpeedCalc(HistoricValue);
                    Console.WriteLine("Set GPU {0} at {1}c (Average temp of {2}c) to a PWM Speed of {3}", 
                        gpu.gpuName, gpu.Temperature, HistoricValue, gpu.FanSpeed);
                    
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


        class GPU
        {
            public ushort VendorID;
            public ushort DeviceID;
            public string path;
            public int gpuNumber;
            public string gpuName;
            public int[] temperatureHistory = new int[5]{1000,1000, 1000, 1000, 1000};
            public int Temperature
            {
                get
                {
                    string temp = File.ReadAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/temp1_input");
                    int temperature = int.Parse(temp) / 1000;
                    byte final = Convert.ToByte(temperature);
                    return final;
                }
            }

            public int FanState
            {
                get
                {
                    string temp = File.ReadAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/pwm1_enable");
                    int mode = int.Parse(temp);
                    return mode;
                }
                set { File.WriteAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/pwm1_enable", value.ToString()); }
            }

            public byte FanSpeed
            {
                get
                {
                    string temp = File.ReadAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/pwm1");
                    byte mode = byte.Parse(temp);
                    return mode;
                }
                set { File.WriteAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/pwm1", value.ToString()); }
            }

            public int MaxPower
            {
                get
                {
                    int v = int.Parse(File.ReadAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/power1_cap_max"));
                    return v;
                }
            }

            public int MinimumPower
            {
                get
                {
                    int v = int.Parse(File.ReadAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/power1_cap_min"));
                    return v;
                }
            }

            public int TargetPower
            {
                get
                {
                    int v = int.Parse(File.ReadAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/power1_cap"));
                    return v;
                }
                set
                {
                    File.WriteAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/power1_cap", value.ToString());
                }
            }
        }

        private class FanStates
        {
            public const int Maximum = 0;
            public const int Manual = 1;
            public const int Automatic = 2;
        }
    }
}
