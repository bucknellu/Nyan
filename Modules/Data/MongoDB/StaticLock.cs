using Nyan.Core.Modules.Data;

namespace Nyan.Modules.Data.MongoDB {
    public class StaticLock<T> where T : MicroEntity<T>
    {
        public static object EntityLock { get; set; } = new object();
    }
}