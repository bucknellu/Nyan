namespace Nyan.Core.Modules.Data.Operators.AnsiSql
{
    internal class SqlNull : Null
    {
        public override string ToString()
        {
            return FieldName + " is null ";
        }
    }
}
