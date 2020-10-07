using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.PortableExecutable;
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
                if (t.StartsWith("card") && char.IsDigit(t[^1]) && t.Length == 5) //Has to be a card, end in a digit, and be 5 characters long.
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
                        p.gpuNumber = int.Parse(t.Substring(t.Length -1, 1));
                        //Until I can find a way to get the actual name of the GPU, this will have to do.
                        p.gpuName = p.gpuNumber.ToString();
                        Gpus.Add(p);
                    }
                }
            }
            //We now have all of our GPU's in a list of GPU's.
            
            while (true)
            {
                foreach (var gpu in Gpus)
                {
                    if (gpu.FanState != FanStates.Manual)
                    {
                        gpu.FanState = FanStates.Manual;
                    }
                    gpu.FanSpeed = FanSpeedCalc(gpu.Temperature);
                    Console.WriteLine("Set GPU {0} at {1}c to a PWM Speed of {2}", gpu.gpuName, gpu.Temperature, gpu.FanSpeed);
                }
                System.Threading.Thread.Sleep(1000);
                Console.Clear();
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
        }
        
        
        class GPU
        {
            public ushort VendorID;
            public ushort DeviceID;
            public string path = "";
            public int gpuNumber;
            public string gpuName;
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
                set
                {
                    File.WriteAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/pwm1_enable", value.ToString());
                }
            }

            public byte FanSpeed
            {
                get
                {
                    string temp = File.ReadAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/pwm1");
                    byte mode = byte.Parse(temp);
                    return mode;
                }
                set
                {
                    File.WriteAllText(path + "/device/hwmon/hwmon" + gpuNumber + "/pwm1", value.ToString());
                }
            }
            
        } 
        public class FanStates
        {
            public const int Maximum = 0;
            public const int Manual = 1;
            public const int Automatic = 2;
        }
    }
}
