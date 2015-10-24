using Nyan.Core.Extensions;
using System;
using System.IO;

namespace Nyan.Core.Modules.Log
{
    public class FileLogProvider : LogProvider
    {
        static string template = "[{0}] {1} - {2}";


        public new void Add(string content, Message.EContentType type = null)
        {


            File.AppendAllText(
                Core.Settings.Current.BaseDirectory + "/log.txt"
                ,
                template.format(DateTime.Now, type.Code, content)
                + Environment.NewLine);
        }
    }
}
