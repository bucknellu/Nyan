using System.IO;
using Nyan.Core.Extensions;

namespace Nyan.Core.Modules.Data.Model
{
    public static class LocalStorageModel
    {
        public static string Save(this object o, string code = "o", string path = null)
        {
            var lpath = path ?? Configuration.DataDirectory;
            var fname = o.GetType().FullName + "-" + code;
            var fullpath = Path.Combine(lpath, fname);

            File.WriteAllText(fullpath, o.ToJson());

            return fullpath;
        }

        public static T Load<T>(string code = "o", string path = null)
        {
            var lpath = path ?? Configuration.DataDirectory;
            var fname = typeof(T).FullName + "-" + code;
            var fullpath = Path.Combine(lpath, fname);
            var src = File.ReadAllText(fullpath);
            return src.FromJson<T>();
        }
    }
}