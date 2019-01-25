using System.IO;

namespace Nyan.Core.Modules.Storage
{
    public static class LocalCache
    {
        public static string BasePath => Configuration.DataDirectory + "\\cache\\local";

        public static void Put(string name, string content)
        {
            if (!Directory.Exists(BasePath)) Directory.CreateDirectory(BasePath);

            var file = BasePath + "\\" + name;

            File.WriteAllText(file, content);
        }

        public static string Get(string name)
        {
            var file = BasePath + "\\" + name;
            return !File.Exists(file) ? null : File.ReadAllText(file);
        }

        public static void Delete(string name)
        {
            var file = BasePath + "\\" + name;
            if (File.Exists(file)) File.Delete(file);
        }
    }
}