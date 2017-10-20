using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using Nyan.Core.Settings;
using MSSystem = System;

namespace Nyan.Core.Modules.Log
{
    public static class Local
    {
        private static bool _canWrite = true;
        private static byte _loopCount;

        static Local()
        {
            Storage = Configuration.DataDirectory + "\\logObjs\\";
            if (!Directory.Exists(Storage))
            {
                Directory.CreateDirectory(Storage);

                var counter = 0;

                while (!Directory.Exists(Storage))
                {
                    Thread.Sleep(250);
                    counter++;

                    if (counter == 40) break; // 10 seconds: Just jump boat.
                }

            }

            // Cleanup local storage before starting.
            if (Directory.Exists(Storage)) MSSystem.Array.ForEach(Directory.GetFiles(Storage), File.Delete);
        }

        public static string Storage { get; }

        private static long GetDirectorySize(string folderPath)
        {
            var di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        private static string LocalToJson(object source)
        {
            var js = new JavaScriptSerializer();
            return js.Serialize(source);
        }

        private static Message JsonToLocal(string source)
        {
            var jsonSerializer = new JavaScriptSerializer();
            return jsonSerializer.Deserialize<Message>(source);
        }

        public static void Store(Message e)
        {

            try
            {
                if (_loopCount == 0) _canWrite = !(GetDirectorySize(Storage) > 1024 * 1024 * 32); // 32MiB per app

                _loopCount++;
                if (_loopCount == 200) _loopCount = 0; //Revalidate folder size every 200 log entries

                var oName = e.Id + ".json";

                if (!_canWrite) return;

                using (var writetext = new StreamWriter(Storage + oName)) { writetext.WriteLine(LocalToJson(e)); }
            }
            catch { } // Ignore errors.
        }

        public static List<string> GetNameInventory(int cutOut = 1024)
        {
            var info = new DirectoryInfo(Storage);
            var files = info.GetFiles().OrderBy(p => p.CreationTime).Take(cutOut).Select(i => i.FullName);
            return files.ToList();
        }

        public static List<Message> GetInventory(int cutOut = 1024)
        {
            var ret = new List<Message>();

            var nameList = GetNameInventory(cutOut);

            nameList.ForEach(i =>
            {
                try
                {
                    using (var readtext = new StreamReader(i))
                    {
                        var readMeText = readtext.ReadLine();
                        var obj = JsonToLocal(readMeText);
                        ret.Add(obj);
                    }
                }
                catch (MSSystem.Exception e) { }
            });

            return ret;
        }

        public static void ManageInventory()
        {
            var msgs = GetInventory();
            foreach (var message in msgs) if (Current.Log.doDispatchCycle(message)) Pull(message);
        }

        private static void Pull(Message message)
        {
            try
            {
                var fName = Storage + message.Id + ".json";
                if (File.Exists(fName)) File.Delete(fName);
            }
            catch { }
        }
    }
}