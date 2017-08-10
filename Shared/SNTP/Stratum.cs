/* Originally sourced from http://www.codeproject.com/Articles/38276/An-SNTP-Client-for-C-and-VB-NET */

namespace TrimbleMonitor.SNTP
{
    /// <summary>
    /// Indicator of the stratum level of a server clock.
    /// </summary>
    public enum Stratum
    {
        /// <summary>
        /// Unspecified or unavailable.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Primary reference (e.g. radio clock).
        /// </summary>
        Primary = 1,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary = 2,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary3 = 3,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary4 = 4,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary5 = 5,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary6 = 6,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary7 = 7,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary8 = 8,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary9 = 9,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary10 = 10,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary11 = 11,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary12 = 12,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary13 = 13,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary14 = 14,

        /// <summary>
        /// Secondary reference (via NTP or SNTP).
        /// </summary>
        Secondary15 = 15,
    }
}
