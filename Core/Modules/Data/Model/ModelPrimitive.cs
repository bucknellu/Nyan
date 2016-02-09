using Nyan.Core.Modules.Data.Adapter;

namespace Nyan.Core.Modules.Data.Model
{
    public abstract class ModelPrimitive<T> where T : ModelPrimitive<T>
    {
        #region Control objects
        private static readonly object AccessLock = new object();

        private T localInstance = null;

        #endregion

        #region Static methods

        #endregion       

        #region Instantiated methods used by Static interface members

        #endregion

        #region Instance methods

        public abstract string GetEntityIdentifier(ModelPrimitive<T> oRef = null);
        public abstract void Insert();
        public abstract bool IsNew();
        public abstract bool IsReadOnly();
        public abstract void OnInsert();
        public abstract void OnRemove();
        public abstract void OnSave(string newIdentifier);
        public abstract void Remove();
        public abstract string Save();
        public abstract string SaveAndGetId();
        public abstract string SaveAndGetId(DynamicParametersPrimitive obj);
        public abstract void SetEntityIdentifier(object value);

        #endregion

        public ModelPrimitive() { }
    }
}
