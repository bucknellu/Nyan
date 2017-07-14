namespace Nyan.Core.Modules.Data.Pipeline
{
    public interface IBeforeActionPipeline : IPipelinePrimitive
    {
        T Process<T>(string action, T current, T source) where T : MicroEntity<T>;
    }
}