namespace Nyan.Core.Modules.Data.Operators.AnsiSql
{
    internal class SqlNotNull : NotNull
    {
        public override string ToString()
        {
            return FieldName + " is not null ";
        }
    }
}
