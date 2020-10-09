using System;
using System.IO;
using System.Globalization;
using PCIIdentificationResolver;
using System.Linq;

namespace BetterFanController
{
    public class GPU
    {
        private string _devicePath;
        private string _hwmonPath;

        public ushort VendorId {get; set;}

        public string VendorName {get; set;}

        public ushort DeviceId {get; set;}

        public string DeviceName {get; set;}

        public int[] TemperatureHistory {get; set;} = new int[5] { 1000, 1000, 1000, 1000, 1000 };

        public int Temperature
        {
            get
            {
                int temperature = int.Parse(ReadHwmonValue("temp1_input")) / 1000;
                return Convert.ToByte(temperature);
            }
        }

        public FanStates FanState
        {
            get => Enum.Parse<FanStates>(ReadHwmonValue("pwm1_enable"));
            set => WriteHwmonValue("pwm1_enable", value.ToString("D")); 
        }

        public byte FanSpeed
        {
            get => byte.Parse(ReadHwmonValue("pwm1"));
            set => WriteHwmonValue("pwm1", value.ToString()); 
        }

        public int MaxPower
        {
            get => int.Parse(ReadHwmonValue("power1_cap_max"));
        }

        public int MinimumPower
        {
            get => int.Parse(ReadHwmonValue("power1_cap_min"));
        }

        public int TargetPower
        {
            get => int.Parse(ReadHwmonValue("power1_cap"));
            set => WriteHwmonValue("power1_cap", value.ToString());
        }

        private GPU() {}

        private GPU(string path)
        {
            _devicePath = Path.Combine(path, "device");

            var vendor = ReadDeviceValue("vendor");
            vendor = vendor.Substring(2, vendor.Length - 2);
            VendorId = ushort.Parse(vendor, NumberStyles.HexNumber);

            var device = ReadDeviceValue("device");
            device = device.Substring(2, device.Length - 2);
            DeviceId = ushort.Parse(device, NumberStyles.HexNumber);

            var deviceInfo = PCIIdentificationDatabase.GetDevice(VendorId, DeviceId);

            VendorName = deviceInfo.ParentVendor.VendorName;
            DeviceName = deviceInfo.DeviceName;

            var parentHwmonDirectory = Path.Combine(_devicePath, "hwmon");
            if(!Directory.Exists(parentHwmonDirectory))
            {
                // No hwmon directory - this isn't a supported card.
                Console.WriteLine($"No hwmon directory found in {_devicePath}!");
                return;
            }

            // Use the first hwmon directory we find
            _hwmonPath = Directory.GetDirectories(parentHwmonDirectory).First();
        }

        public static GPU LoadFromPath(string path)
        {
            return new GPU(path);
        }

        private string ReadDeviceValue(string name)
        {
            return File.ReadAllText(Path.Combine(_devicePath, name));
        }

        private string ReadHwmonValue(string name)
        {
            return File.ReadAllText(Path.Combine(_hwmonPath, name));
        }

        private void WriteHwmonValue(string name, string value)
        {
            File.WriteAllText(Path.Combine(_hwmonPath, name), value);
        }
    }
}