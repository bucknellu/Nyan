namespace Nyan.Core.Modules.Data.Contracts {
    public class EntityReference
    {
        public string Id;
        public string Label;
        public EntityReference() { }

        public EntityReference(string id, string label)
        {
            Id = id;
            Label = label;
        }
        public EntityReference(long id, string label)
        {
            Id = id.ToString();
            Label = label;
        }
    }
}