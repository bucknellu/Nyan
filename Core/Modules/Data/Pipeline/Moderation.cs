namespace Nyan.Core.Modules.Data.Pipeline
{
    public class Moderation : IBeforeActionPipeline
    {
        public T Process<T>(Support.EAction action, T current, T source) where T : MicroEntity<T> { return current; }
    }
}