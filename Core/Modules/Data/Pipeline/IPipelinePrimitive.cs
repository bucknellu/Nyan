using System.Collections.Generic;

namespace Nyan.Core.Modules.Data.Pipeline
{
    public interface IPipelinePrimitive
    {
        Dictionary<string, object> Headers<T>() where T : MicroEntity<T>;
    }
}