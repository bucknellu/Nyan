using System;

namespace Nyan.Core.Modules.Environment
{
    public class ProbeItem
    {
        public string Locator { get; set; }
        public string Source { get; set; }
    }

    public interface IEnvironmentProvider
    {
        IEnvironmentDescriptor Current { get; set; }
        string CurrentCode { get; }
        ProbeItem Probe { get; set; }
        void ResetToDefault();
        void Shutdown();
        IEnvironmentDescriptor Get(string serverName);
        event EventHandler EnvironmentChanged;
    }
}