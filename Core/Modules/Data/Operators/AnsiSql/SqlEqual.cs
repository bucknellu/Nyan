namespace Nyan.Core.Modules.Data.Operators.AnsiSql
{
    internal class SqlEqual : Equal
    {
        public override string ToString()
        {
            return FieldName + " = " + FieldValue;
        }
    }
}
