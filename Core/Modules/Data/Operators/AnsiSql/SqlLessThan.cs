namespace Nyan.Core.Modules.Data.Operators.AnsiSql
{
    internal class SqlLessThan : LessThan
    {
        public override string ToString()
        {
            return FieldName + " < " + FieldValue;
        }
    }
}
