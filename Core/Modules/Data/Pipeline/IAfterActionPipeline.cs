namespace Nyan.Core.Modules.Data.Pipeline
{
    public interface IAfterActionPipeline : IPipelinePrimitive
    {
        void Process<T>(Support.EAction action, T current, T source) where T : MicroEntity<T>;
    }
}