namespace Nyan.Core.Modules.Environment
{
    public sealed class DefaultEnvironmentDescriptor : IEnvironmentDescriptor
    {
        //The default Descriptor handles only one environment.
        public static readonly IEnvironmentDescriptor Standard = new DefaultEnvironmentDescriptor(0, "STA", "Standard");

        private DefaultEnvironmentDescriptor(int value, string code, string name)
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