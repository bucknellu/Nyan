namespace Nyan.Core.Modules.Data.Operators
{
    internal abstract class NotNull : INamedOperator
    {
        public string Prefix { get; set; }
        public string FieldName { get; set; }
    }
}
