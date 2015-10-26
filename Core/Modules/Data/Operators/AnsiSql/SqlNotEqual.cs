namespace Nyan.Core.Modules.Data.Operators.AnsiSql
{
    internal class SqlNotEqual : NotEqual
    {
        public override string ToString()
        {
            return FieldName + " <> " + FieldValue;
        }
    }
}
