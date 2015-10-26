namespace Nyan.Core.Modules.Data.Operators.AnsiSql
{
    internal class SqlLessOrEqualThan : LessOrEqualThan
    {
        public override string ToString()
        {
            return FieldName + " <= " + FieldValue;
        }
    }
}
