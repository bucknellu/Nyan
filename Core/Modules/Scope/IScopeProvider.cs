using System;

namespace Nyan.Core.Modules.Scope
{
    public interface IScopeProvider
    {
        IScopeDescriptor Current { get; set; }
        string CurrentCode { get; }
        string Probe { get; }
        void ResetToDefault();
        IScopeDescriptor Get(string serverName);
        event EventHandler EnvironmentChanged;
    }
}