using System;

namespace Nyan.Core.Modules.Environment
{
    public interface IEnvironmentProvider
    {
        IEnvironmentDescriptor Current { get; set; }
        string CurrentCode { get; }
        string Probe { get; }
        void ResetToDefault();
        IEnvironmentDescriptor Get(string serverName);
        event EventHandler EnvironmentChanged;
    }
}