/* Originally sourced from http://www.codeproject.com/Articles/38276/An-SNTP-Client-for-C-and-VB-NET */

namespace SNTP
{
    /// <summary>
    /// Indicator of the NTP/SNTP version number.
    /// </summary>
    public enum VersionNumber
    {
        /// <summary>
        /// Version 3 (IPv4 only).
        /// </summary>
        Version3 = 3,

        /// <summary>
        /// Version 4 (IPv4, IPv6 and OSI).
        /// </summary>
        Version4 = 4,
    }
}
