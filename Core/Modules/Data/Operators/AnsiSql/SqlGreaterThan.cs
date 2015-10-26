namespace Nyan.Core.Modules.Data.Operators.AnsiSql
{
    internal class SqlGreaterThan : GreaterThan
    {
        public override string ToString()
        {
            return FieldName + " > " + FieldValue;
        }
    }
}
