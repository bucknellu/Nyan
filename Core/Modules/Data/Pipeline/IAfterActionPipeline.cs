namespace Nyan.Core.Modules.Data.Pipeline
{
    public interface IAfterActionPipeline : IPipelinePrimitive
    {
        void Process<T>(string action, T current, T source) where T : MicroEntity<T>;
    }
}