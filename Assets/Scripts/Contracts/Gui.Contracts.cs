using System.Collections.Generic;


namespace Contracts
{
    public interface IGUIHub
    {
        // Sends a list of configuration file names to the GUI for display
        public void pushConfigList(List<string> configFileNames);
    }
}