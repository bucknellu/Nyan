using System;

namespace Nyan.Core.Modules.Environment
{
    public class DefaultEnvironmentProvider : IEnvironmentProvider
    {
        public IEnvironmentDescriptor Current
        {
            get { return DefaultEnvironmentDescriptor.Standard; }
            set { throw new NotImplementedException(); }
        }

        public string CurrentCode { get { return DefaultEnvironmentDescriptor.Standard.Code; } }

        ProbeItem IEnvironmentProvider.Probe { get; }

        public void ResetToDefault()
        {
            throw new NotImplementedException();
        }

        public void Shutdown() { }

        public IEnvironmentDescriptor Get(string serverName)
        {
            throw new NotImplementedException();
        }

        public event EventHandler EnvironmentChanged;
    }
}