using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Settings
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PackagePriorityAttribute : Attribute
    {
        public int Level { get; set; }

    }
}
