using System.Collections.Generic;

namespace BetterFanController
{
    public class Configuration
    {
        public List<GpuConfigs> GpuConfigInfo = new List<GpuConfigs>(); 

        public int LongestName
        {
            get
            {
                int longestName = 0;
                foreach (var gpu in GpuConfigInfo)
                {
                    if (longestName < gpu.NameOverride.Length)
                    {
                        longestName = gpu.NameOverride.Length;
                    }
                }
                return longestName;
            }
        }
    }

    public class GpuConfigs
    {
        public string NameOverride;
        public int DeviceID;
        public int TargetPower;
        public int MaxPowerDoNotChange;
        public int MaxTemperature = 50;
        public int MinimumTemperature = 30;
    }
}