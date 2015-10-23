namespace Nyan.Core.Modules.Data.Operators
{
    internal abstract class Null : INamedOperator
    {
        public string Prefix { get; set; }
        public string FieldName { get; set; }
    }
}
