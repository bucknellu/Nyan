using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Nyan.Core.Factories
{
    public class Tree<T>
    {
        private readonly bool _added = false;
        internal List<Tree<T>> _children = new List<Tree<T>>();
        internal Dictionary<string, Tree<T>> _hashTable = new Dictionary<string, Tree<T>>();
        private T _item;
        private string _key;
        internal Tree<T> _parent = null;

        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                TrySetMap();
            }
        }
        public T Item
        {
            get => _item;
            set
            {
                _item = value;
                TrySetMap();
            }
        }


        public ReadOnlyCollection<Tree<T>> Children => _children.AsReadOnly();
        [JsonIgnore]
        public Tree<T> Parent => _parent;
        [JsonIgnore]
        public ReadOnlyDictionary<string, Tree<T>> Map => new ReadOnlyDictionary<string, Tree<T>>(_hashTable);

        private void TrySetMap()
        {
            if (_added) return;
            if (Key == null) return;
            if (_item == null) return;

            AddToHash(Key, this);
        }

        public void AddChild(string key, T item)
        {
            var t = new Tree<T> {Key = key, Item = item, _parent = this, _hashTable = _hashTable};

            AddToHash(key, t);
            if (_children == null) _children = new List<Tree<T>>();

            _children.Add(t);
        }

        private void AddToHash(string key, Tree<T> item)
        {
            if (_parent == null) _hashTable.Add(key, item);
            else _parent.AddToHash(key, item);
        }

        public Tree<T> ByPath(Dictionary<string, T> seedPath)
        {
            var refTree = this;

            foreach (var sp in seedPath)
            {
                if (!refTree.Map.ContainsKey(sp.Key)) refTree.AddChild(sp.Key, sp.Value);

                refTree = refTree.Map[sp.Key];
            }

            return refTree;
        }
    }
}