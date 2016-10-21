namespace Nyan.Core.Startup
{
    public interface IServiceDefinition
    {
        string Name { get; }
        string Description { get; }
        void Initialize();
        void Start();
        void Stop();
    }
}