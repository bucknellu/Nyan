using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Data.Adapter;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;
using Nyan.Core.Wrappers;

namespace Nyan.Core.Modules.Data.Maintenance
{
    public static class Definition
    {
        [Flags]
        public enum DdlContent
        {
            None = 0,
            Schema = 1,
            Data = 2,
            All = Schema | Data
        }

        public static void WipeModelsFromDisk(string path )
        {
            if (path == null)
                path = Configuration.DataDirectory + "\\model\\";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Array.ForEach(Directory.GetFiles(path), File.Delete);

        }

        public static List<DataAdapterPrimitive.ModelDefinition> GetModels(bool limitToMainAssembly = true)
        {
            var probe = typeof(MicroEntity<>);

            var objCol = Management.GetClassesByBaseClass(probe, limitToMainAssembly);

            return objCol
                .Select(obj => new StaticMembersDynamicWrapper(obj))
                .Select(dynObj => ((dynamic) dynObj).ModelDefinition)
                .Cast<DataAdapterPrimitive.ModelDefinition>()
                .ToList();
        }

        public static string RenderModelsToDisk(bool limitToMainAssembly = true, string path = null)
        {

            if (path == null)
                path = Configuration.DataDirectory + "\\model\\";

            WipeModelsFromDisk(path);

            foreach (var md in GetModels(limitToMainAssembly).Where(md => md.Available))
            {
                Current.Log.Add("[" + md.Type.Name + "]: Rendering model to disk", Message.EContentType.Maintenance);

                if (md.Schema != null)
                    File.WriteAllText(path + "schema-{1}-{0}-[{2}].sql".format(md.EnvironmentCode, md.AdapterType, md.Type.Name), md.Schema);

                if (md.Data != null)
                    File.WriteAllText(path + "data-{1}-{0}-[{2}].sql".format(md.EnvironmentCode, md.AdapterType, md.Type.Name), md.Data);
            }

            Current.Log.Add("[RenderModelsToDisk]: Models available at " + path, Message.EContentType.Maintenance);

            return path;
        }
    }
}