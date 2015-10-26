namespace Nyan.Core.Modules.Data.Operators.AnsiSql
{
    internal class SqlGreaterOrEqualThan : GreaterOrEqualThan
    {
        public override string ToString()
        {
            return FieldName + " >= " + FieldValue;
        }
    }
}
