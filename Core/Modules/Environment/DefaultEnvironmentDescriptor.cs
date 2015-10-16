using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Environment
{
    public sealed class DefaultEnvironmentDescriptor : IEnvironmentDescriptor
    {
        //The default Descriptor handles only one environment.
        public static readonly IEnvironmentDescriptor Standard = new DefaultEnvironmentDescriptor(0, "STD", "Standard");

        public string Name { get; private set; }
        public string Code { get; private set; }
        public int Value { get; private set; }

        private DefaultEnvironmentDescriptor(int value, string code, string name)
        {
            Value = value;
            Name = name;
            Code = code;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
