using System.Collections.Generic;

namespace BetterFanController
{
    public class Configuation
    {
        public List<GpuConfigs> GpuConfigInfo = new List<GpuConfigs>(); 
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