using System;
using Microsoft.SPOT;

namespace TrimbleMonitor.Thunderbolt
{
    #region Delegates

    internal delegate void ThunderBoltEventHandler(object sender, EventArgs e);
    internal delegate void VersionInfoEventHandler(object sender, VersionInfoEventArgs e);
    internal delegate void GpsTimeReceivedEventHandler(object sender, GPSTimeInfoEventArgs e);

    #endregion

    class VersionInfoEventArgs : EventArgs
    {
        private VersionInfo ver;

        public VersionInfo VerInfo
        {
            get { return ver; }
        }

        public VersionInfoEventArgs(VersionInfo v)
        {
            ver = v;
        }
    }

    class GPSTimeInfoEventArgs : EventArgs
    {
        private Single time;

        public Single Time
        {
            get { return time; }
            set { time = value; }
        }
        private Int16 week;

        public Int16 Week
        {
            get { return week; }
            set { week = value; }
        }
        private Single offset;

        public Single Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public GPSTimeInfoEventArgs(Single t, Int16 w, Single ofs)
        {
            time = t;
            week = w;
            offset = ofs;
        }
    }
}
