namespace Nyan.Core.Modules.Data.Operators
{
    internal class NamedOperator : INamedOperator
    {
        public string Prefix { get; set; }
        public string FieldName { get; set; }
        public object FieldValue { get; set; }
    }
}
