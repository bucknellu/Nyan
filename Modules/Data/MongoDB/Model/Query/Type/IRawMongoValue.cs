using Newtonsoft.Json.Linq;

namespace Nyan.Modules.Data.MongoDB.Model.Query.Type
{
    public interface IRawMongoValue
    {
        JRaw Value { get; set; }
    }
}