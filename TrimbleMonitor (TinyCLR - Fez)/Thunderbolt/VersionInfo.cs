using System;

namespace TrimbleMonitor.Thunderbolt
{
    class VersionInfo
    {
        public VersionInfo()
        {
            SerialNumber = "";
            VersionString = "";
            Date = new DateTime(DateTime.MinValue.Ticks);
        }

        public string SerialNumber { get; set; }

        public uint Code { get; set; }

        public string VersionString { get; set; }

        public int Major { get; set; }

        public int Minor { get; set; }

        public DateTime Date { get; set; }
    }
}
