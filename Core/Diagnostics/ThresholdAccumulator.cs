using System;
using System.Collections.Generic;
using System.Threading;
using Nyan.Core.Modules.Data;
using Nyan.Core.Process;
using Nyan.Core.Settings;

namespace Nyan.Core.Diagnostics
{
    public class TimedThreshold<T, TU> where TU : MicroEntity<TU>
    {
        private readonly object _cacheFlushlock = new object();
        private readonly Action<Dictionary<Tuple<T, TU>, int>> _onThreshold;
        private readonly TimeSpan _timeSpan;
        public readonly Dictionary<Tuple<T, TU>, int> Occurences = new Dictionary<Tuple<T, TU>, int>();
        public readonly Dictionary<T, Tuple<T, TU>> ToC = new Dictionary<T, Tuple<T, TU>>();
        private DateTime _lastOccurence = DateTime.Now;

        public TimedThreshold(TimeSpan timeSpan, Action<Dictionary<Tuple<T, TU>, int>> onThreshold)
        {
            _timeSpan = timeSpan;
            _onThreshold = onThreshold;
            Sequences.ShutdownActions.Add(FlushCache);
        }

        public void Pip(T type, TU holder) { Pip(type, holder, 1); }

        public void Pip(T type, TU holder, int count)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    lock (_cacheFlushlock)
                    {
                        if (!ToC.ContainsKey(type))
                        {
                            ToC.Add(type, new Tuple<T, TU>(type, holder));
                            Occurences.Add(ToC[type], 0);
                        }

                        Occurences[ToC[type]] += count;

                        if (_lastOccurence.Add(_timeSpan) >= DateTime.Now) return;

                        FlushCache();
                    }
                } catch (Exception e) { Current.Log.Add(e); }
            }, null);
        }

        private void FlushCache()
        {
            try { _onThreshold(Occurences); } catch (Exception e) { Current.Log.Add(e); }

            _lastOccurence = DateTime.Now;
            Occurences.Clear();
            ToC.Clear();
        }
    }
}