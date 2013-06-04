/* Originally sourced from http://www.codeproject.com/Articles/38276/An-SNTP-Client-for-C-and-VB-NET */

namespace SNTP
{
    /// <summary>
    /// Indicator of the mode of operation.
    /// In unicast and anycast modes, the client sets this field to 3 (client) in the request
    /// and the server sets it to 4 (server) in the reply.
    /// In multicast mode, the server sets this field to 5 (broadcast).
    /// </summary>
    public enum Mode
    {
        /// <summary>
        /// Reserved.
        /// </summary>
        Reserved = 0,

        /// <summary>
        /// Symmetric active.
        /// </summary>
        SymmetricActive = 1,

        /// <summary>
        /// Symmetric passive.
        /// </summary>
        SymmetricPassive = 2,

        /// <summary>
        /// Client.
        /// </summary>
        Client = 3,

        /// <summary>
        /// Server.
        /// </summary>
        Server = 4,

        /// <summary>
        /// Broadcast.
        /// </summary>
        Broadcast = 5,

        /// <summary>
        /// Reserved for NTP control message.
        /// </summary>
        ReservedNTPControl = 6,

        /// <summary>
        /// Reserved for private use.
        /// </summary>
        ReservedPrivate = 7
    }
}
