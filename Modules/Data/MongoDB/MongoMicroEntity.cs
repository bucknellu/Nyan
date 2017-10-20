using MongoDB.Bson.Serialization.Attributes;
using Nyan.Core.Modules.Data;

namespace Nyan.Modules.Data.MongoDB
{
    [BsonNoId]
    public abstract class MongoMicroEntity<T> : MicroEntity<T> where T : MicroEntity<T>
    {
        public static void PushSet(string setIdentifier)
        {
            var interceptor = (MongoDbinterceptor)Statements.Interceptor;
            var destinationName = interceptor.SourceCollection + "#version:" + setIdentifier;

            PushSet(interceptor.SourceCollection, destinationName);
        }
        public static void PullSet(string setIdentifier)
        {
            var interceptor = (MongoDbinterceptor)Statements.Interceptor;
            var destinationName = interceptor.SourceCollection + "#version:" + setIdentifier;
            interceptor.CopyTo(destinationName, interceptor.SourceCollection, false);
        }

        public static void PushSet(string localSet, string storageSet, bool isPartialstorageSet= false)
        {

            if (isPartialstorageSet) storageSet = localSet + "#version:" + storageSet;

            var interceptor = (MongoDbinterceptor)Statements.Interceptor;
            interceptor.CopyTo(localSet, storageSet, true);
        }

        public static void PullSet(string localSet, string storageSet, bool isPartialstorageSet = false)
        {
            if (isPartialstorageSet) storageSet = localSet + "#version:" + storageSet;
            var interceptor = (MongoDbinterceptor)Statements.Interceptor;
            interceptor.CopyTo(storageSet, localSet, true);
        }



    }
}