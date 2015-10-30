namespace Nyan.Core.Modules.Scope
{
    public sealed class DefaultScopeDescriptor : IScopeDescriptor
    {
        //The default Descriptor handles only one environment.
        public static readonly IScopeDescriptor Standard = new DefaultScopeDescriptor(0, "STD", "Standard");

        private DefaultScopeDescriptor(int value, string code, string name)
        {
            Value = value;
            Name = name;
            Code = code;
        }

        public string Name { get; private set; }
        public string Code { get; private set; }
        public int Value { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}