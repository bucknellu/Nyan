using System;
using System.Diagnostics;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Maintenance
{
    public class Clicker
    {
        private long _pCount;
        private long _pIndex;
        private string _pMessage;
        private int _pNotifySlice;

        private Stopwatch _s;

        public Clicker(string pMessage, long pCount) { Start(pMessage, pCount); }

        public void Start(string pMessage, long pCount, int pNotifySlice = 100)
        {
            _pMessage = pMessage;
            _pCount = pCount;
            _pNotifySlice = pNotifySlice;

            _s = new Stopwatch();
            _s.Start();

            Current.Log.Add(_pMessage + ": START", Message.EContentType.Info);
        }

        public void Click()
        {
            _pIndex++;
            if (_pIndex % _pNotifySlice != 0) return;

            var part = (double) _pIndex / _pCount;
            var invPart = TimeSpan.FromMilliseconds(_s.ElapsedMilliseconds * (1 / part));

            Current.Log.Add($"    {_pIndex}/{_pCount} ({part:P2} / {invPart.Subtract(_s.Elapsed)} left, ~{invPart} Total)", Message.EContentType.MoreInfo);
        }
    }
}