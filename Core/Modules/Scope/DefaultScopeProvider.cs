using System;

namespace Nyan.Core.Modules.Scope
{
    public class DefaultScopeProvider : IScopeProvider
    {
        public IScopeDescriptor Current
        {
            get { return DefaultScopeDescriptor.Standard; }
            set { throw new NotImplementedException(); }
        }

        public string CurrentCode
        {
            get { return DefaultScopeDescriptor.Standard.Code; }
        }

        public string Probe { get; private set; }

        public void ResetToDefault()
        {
            throw new NotImplementedException();
        }

        public void Shutdown() {  }

        public IScopeDescriptor Get(string serverName)
        {
            throw new NotImplementedException();
        }

        public event EventHandler EnvironmentChanged;
    }
}