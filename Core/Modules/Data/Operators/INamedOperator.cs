namespace Nyan.Core.Modules.Data.Operators
{
    internal interface INamedOperator : IOperator
    {
        string Prefix { get; set; }
        string FieldName { get; set; }
    }
}
