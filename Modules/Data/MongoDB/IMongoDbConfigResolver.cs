namespace Nyan.Modules.Data.MongoDB
{
    public interface IMongoDbConfigResolver
    {
        string GetDatabaseName();
    }
}