using System;
using System.Diagnostics;
using System.Globalization;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Maintenance
{
    public class Clicker
    {
        private long _pIndex;
        private string _pMessage;
        private int _pNotifySlice;

        private static readonly NumberFormatInfo Format = new NumberFormatInfo { PercentPositivePattern = 1, PercentNegativePattern = 1 };

        private Stopwatch _s;

        public Clicker(string pMessage, long pCount) { Start(pMessage, pCount); }

        public long Count { get; private set; }

        public void Start(string pMessage, long pCount, int pNotifySlice = 100)
        {
            _pMessage = pMessage;
            Count = pCount;
            _pNotifySlice = pNotifySlice;

            _s = new Stopwatch();
            _s.Start();

            Current.Log.Add($"{_pMessage}: START ({pCount}) items", Message.EContentType.Info);
        }

        public void Click()
        {
            _pIndex++;
            if (_pIndex % _pNotifySlice != 0) return;

            var part = ((double) _pIndex / Count);
            var partStr = part.ToString("P2", Format).PadLeft(7);

            var invPart = TimeSpan.FromMilliseconds(_s.ElapsedMilliseconds * (1 / part));

            var charSlots = Count.ToString().Length;

            var sIndex = _pIndex.ToString().PadLeft(charSlots);

            var currT = _s.Elapsed.ToString(@"\:hh\:mm\:ss");
            var leftT = invPart.Subtract(_s.Elapsed).ToString(@"\:hh\:mm\:ss");
            var totlT = invPart.ToString(@"\:hh\:mm\:ss");

            Current.Log.Add($"    {_pMessage}: {sIndex}/{Count} ({partStr} | E{currT} L{leftT} T{totlT})", Message.EContentType.MoreInfo);
        }

        public void End()
        {
            _s.Stop();
            var regPerSec = Count / ((double) _s.ElapsedMilliseconds / 1000);
            Current.Log.Add($"    {_pMessage}: END ({_s.Elapsed} elapsed, {regPerSec:F2} items/sec)", Message.EContentType.Info);
        }
    }
}