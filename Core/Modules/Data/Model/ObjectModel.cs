using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nyan.Core.Modules.Data.Adapter;

namespace Nyan.Core.Modules.Data.Model
{
    public class ObjectModel : ModelPrimitive<ObjectModel>
    {
        public override string GetEntityIdentifier(ModelPrimitive<ObjectModel> oRef = null)
        {
            throw new NotImplementedException();
        }

        public override void Insert()
        {
            throw new NotImplementedException();
        }

        public override bool IsNew()
        {
            throw new NotImplementedException();
        }

        public override bool IsReadOnly()
        {
            throw new NotImplementedException();
        }

        public override void OnInsert()
        {
            throw new NotImplementedException();
        }

        public override void OnRemove()
        {
            throw new NotImplementedException();
        }

        public override void OnSave(string newIdentifier)
        {
            throw new NotImplementedException();
        }

        public override void Remove()
        {
            throw new NotImplementedException();
        }

        public override string Save()
        {
            throw new NotImplementedException();
        }

        public override string SaveAndGetId()
        {
            throw new NotImplementedException();
        }

        public override string SaveAndGetId(DynamicParametersPrimitive obj)
        {
            throw new NotImplementedException();
        }

        public override void SetEntityIdentifier(object value)
        {
            throw new NotImplementedException();
        }
    }
}
