using Nyan.Core.Modules.Cache;

namespace Nyan.Modules.Cache.Redis
{
    public class RedisCacheConfiguration : ICacheConfiguration
    {
        public string ConnectionString { get; set; }
        public int DatabaseIndex { get; set; }
    }
}