using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CacheEntityAttribute : Attribute
    {
        // ReSharper restore InconsistentNaming
        [NotNull]
        public string IdentifierPropertyName;
    }
}
