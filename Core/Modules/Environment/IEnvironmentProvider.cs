using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
