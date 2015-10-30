namespace Nyan.Core.Modules.Scope
{
    public interface IScopeDescriptor
    {
        string Name { get; }
        string Code { get; }
        int Value { get; }
        string ToString();
    }
}