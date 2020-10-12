using System.Collections.Generic;

namespace BetterFanController
{
    public interface IConfigurationProvider
    {
        Configuration Configuration{ get; }

        void SaveConfiguration();
    }

}