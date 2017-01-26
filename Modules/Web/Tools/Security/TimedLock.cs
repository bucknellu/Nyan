using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Nyan.Modules.Web.Tools.Security
{
    public class TimedLock
    {
        public enum TimedLockMode
        {
            SaturationAndCount,
            SaturationOnly,
            CountOnly
        }
        private readonly object _accesslock = new object();

        private readonly List<TimeLockEntry> _entries = new List<TimeLockEntry>();
        private readonly object _writelock = new object();
        public TimedLockMode Mode = TimedLockMode.SaturationAndCount;

        public TimedLock()
        {
            TimeOut = 5*60; // 5 minutes
            MaxHits = 10;
            SaturationTimeFrame = 10; // 10 seconds;
            SaturationMaxHits = 5;
            CoolDownPeriod = 3*60; // 3 minutes;
        }

        public TimedLock(TimedLockMode pMode)
        {
            TimeOut = 5*60; // 5 minutes
            MaxHits = 10;
            SaturationTimeFrame = 10; // 10 seconds;
            SaturationMaxHits = 5;
            CoolDownPeriod = 3*60; // 3 minutes;
            Mode = pMode;
        }

        /// <summary>
        ///     Specifies the the amount of minutes for a specific lookup entry to be dropped.
        /// </summary>
        /// <value>
        ///     The time out in minutes.
        /// </value>
        /// <remarks>
        ///     If a query happens before the timeout is over, the hit count will increase and reset the counter.
        /// </remarks>
        public int TimeOut { get; set; }

        public int MaxHits { get; set; }
        public int SaturationTimeFrame { get; set; }
        public int SaturationMaxHits { get; set; }
        public int CoolDownPeriod { get; set; }

        public TimeLockResult CheckAvailability(string key) { return _Check(key, false); }


        /// <summary>
        ///     Checks if the specified key can be accessed or not.
        /// </summary>
        /// <param name="key">The key value.</param>
        /// <returns></returns>
        public TimeLockResult HitFailure(string key)
        {
            var ret = _Check(key, true);
            return ret;
        }

        public void HitSuccess(string key)
        {
            if (key == null) return;
            key = key.Trim().ToLower();
            if (key == "") return;

            lock (_accesslock) // Prevents other threads from trying to do the same at the same time, blocking them.
            {
                TimeLockEntry probe = null;
                foreach (var item in _entries) // Checking if the key exists.
                {
                    if (!item.Key.Equals(key)) continue;
                    probe = item;
                    break;
                }

                if (probe == null) return;
                //Entry located; since it was a good hit, just remove it from the collection.
                _entries.Remove(probe);
            }
        }

        private TimeLockResult _Check(string key, bool pMustAdd)
        {
            if (key == null) return new TimeLockResult(false, "No key specified.");
            key = key.Trim().ToLower();
            if (key == "") return new TimeLockResult(false, "No key specified.");

            lock (_writelock) // Prevents other threads form trying to do the same at the same time, blocking them.
            {
                TimeLockEntry probe = null;

                foreach (var item in _entries) // Checking if the key already exists.
                {
                    if (!item.Key.Equals(key)) continue;
                    probe = item;
                    break;
                }

                if (probe != null)
                    if (probe.LastIteration.ElapsedMilliseconds > TimeOut*1000)
                    {
                        _entries.Remove(probe);
                        probe = null;
                    }

                if (probe == null) // Creates a new entry for this key.
                {
                    probe = new TimeLockEntry
                    {
                        Key = key,
                        MaxHits = MaxHits,
                        SaturationMaxHits = SaturationMaxHits,
                        SaturationTimeFrame = SaturationTimeFrame,
                        CoolDownPeriod = CoolDownPeriod,
                        Mode = Mode
                    };

                    _entries.Add(probe);
                }

                var ret = probe.Hit(pMustAdd);

                return ret;
            }
        }

        internal class TimeLockEntry
        {
            internal Stopwatch CoolDownLock = new Stopwatch();
            internal int CoolDownPeriod;
            internal int HitCount;
            internal List<DateTime> Iterations = new List<DateTime>();
            internal string Key;
            internal Stopwatch LastIteration = new Stopwatch();
            internal int MaxHits;
            internal TimedLockMode Mode = TimedLockMode.SaturationAndCount;
            internal int SaturationMaxHits;
            internal int SaturationTimeFrame;

            internal TimeLockResult Hit(bool pMustAdd)
            {
                if (!pMustAdd) LastIteration.Reset();

                if (CoolDownLock.IsRunning) // This entry is on Lock Cooldown
                    if (CoolDownLock.ElapsedMilliseconds > CoolDownPeriod*1000)
                    {
                        CoolDownLock.Stop();
                        CoolDownLock.Reset();
                        HitCount = 0;
                        Iterations = new List<DateTime>();
                    }
                    else
                    {
                        var elapsed = (CoolDownPeriod - CoolDownLock.ElapsedMilliseconds/1000).ToString();

                        return new TimeLockResult(false, "On cooldown - " + elapsed + "s.");
                    }

                if (pMustAdd)
                {
                    HitCount++;
                    Iterations.Add(DateTime.Now);
                }

                if (
                    (Mode == TimedLockMode.CountOnly) ||
                    (Mode == TimedLockMode.SaturationAndCount)
                )

                    if (HitCount > MaxHits)
                    {
                        CoolDownLock.Reset();
                        CoolDownLock.Start();
                        return new TimeLockResult(false, "Max hits reached (period).");
                    }

                if ((Mode != TimedLockMode.SaturationOnly) && (Mode != TimedLockMode.SaturationAndCount))
                    return new TimeLockResult(true,
                        "Success;" +
                        HitCount + "/" + MaxHits + "  overall;" +
                        Iterations.Count + "/" + SaturationMaxHits + " under " + SaturationTimeFrame + "s");

                // strip away all iterations over [this.SaturationTimeFrame] seconds
                Iterations = Iterations.FindAll(a => a > DateTime.Now.AddMilliseconds(SaturationTimeFrame*-1000));
                if (!pMustAdd)
                    return new TimeLockResult(true,
                        "Success;" +
                        HitCount + "/" + MaxHits + "  overall;" +
                        Iterations.Count + "/" + SaturationMaxHits + " under " + SaturationTimeFrame + "s");

                if (Iterations.Count <= SaturationMaxHits)
                    return new TimeLockResult(true,
                        "Success;" +
                        HitCount + "/" + MaxHits + "  overall;" +
                        Iterations.Count + "/" + SaturationMaxHits + " under " + SaturationTimeFrame + "s");

                CoolDownLock.Reset();
                CoolDownLock.Start();
                return new TimeLockResult(false, "Max hits reached (saturation).");
            }
        }

        public class TimeLockResult
        {
            public string Reason;
            public bool Status;

            internal TimeLockResult() { }

            internal TimeLockResult(bool pStatus, string pReason)
            {
                Status = pStatus;
                Reason = pReason;
            }
        }
    }
}