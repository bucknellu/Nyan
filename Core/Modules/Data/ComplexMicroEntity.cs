using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Data
{
    public class ComplexMicroEntity<T, TU>
        where T : ComplexMicroEntity<T, TU>, new()
        where TU : MicroEntity<TU>
    {
        public static T Get(long identifier)
        {
            return Get(identifier.ToString(CultureInfo.InvariantCulture));
        }

        public static T Get(string identifier, string referenceField = null)
        {
            var ret = new T();
            TU probe;

            if (referenceField == null)
                probe = MicroEntity<TU>.Get(identifier);
            else
            {
                var lprobe = MicroEntity<TU>.ReferenceQueryByField(referenceField, identifier).ToList();
                if (lprobe.Count > 0)
                    probe = lprobe[0];
                else
                    return null;
            }

            var ret2 = new T();

            try { ret2 = ret.CastToComplexType((probe)); } catch (Exception ee)
            {
                Current.Log.Add(ee);
            }

            return ret2;
        }

        public static T Get(TU probe)
        {
            return new T().CastToComplexType(probe);
        }

        public static List<T> ConvertFromBaseList(List<TU> source)
        {
            return source.Select(x => new T().CastToComplexType(x)).ToList();
        }

        public virtual T CastToComplexType(TU probe)
        {
            //Must ALWAYS be overridden by implementing Class.
            throw new NotImplementedException();
        }
    }
}