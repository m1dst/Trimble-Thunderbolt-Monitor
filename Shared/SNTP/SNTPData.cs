/* Originally sourced from http://www.codeproject.com/Articles/38276/An-SNTP-Client-for-C-and-VB-NET */

using System;

namespace TrimbleMonitor.SNTP
{
    /// <summary>
    /// A class that represents a SNTP packet.
    /// See http://www.faqs.org/rfcs/rfc2030.html for full details of protocol.
    /// </summary>
    public class SNTPData
    {
        #region Fields

        private static readonly DateTime Epoch = new DateTime(1900, 1, 1);
        private const int LeapIndicatorLength = 4;
        private const byte LeapIndicatorMask = 0xC0;
        private const byte LeapIndicatorOffset = 6;
        /// <summary>
        /// The maximum number of bytes in a SNTP packet.
        /// </summary>
        public const int MaximumLength = 68;
        /// <summary>
        /// The minimum number of bytes in a SNTP packet.
        /// </summary>
        public const int MinimumLength = 48;
        private const byte ModeComplementMask = 0xF8;
        private const int ModeLength = 8;
        private const byte ModeMask = 0x07;
        private const int originateIndex = 24;
        private const int receiveIndex = 32;
        private const int referenceIdentifierOffset = 12;
        private const int referenceIndex = 16;
        private const int StratumLength = 16;
        /// <summary>
        /// Represents the number of ticks in 1 second.
        /// </summary>
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;
        private const int transmitIndex = 40;
        private const byte VersionNumberComplementMask = 0xC7;
        private const int VersionNumberLength = 8;
        private const byte VersionNumberMask = 0x38;
        private const byte VersionNumberOffset = 3;

        #endregion Fields

        #region Constructors

        internal SNTPData(byte[] bytearray)
        {
            if (bytearray.Length >= MinimumLength && bytearray.Length <= MaximumLength)
                Data = bytearray;
            else
                throw new ArgumentOutOfRangeException(
                    "Byte Array",
                    "Byte array must have a length between " + MinimumLength + " and " + MaximumLength + ".");
        }

        internal SNTPData()
            : this(new byte[48])
        { }

        #endregion Constructors

        #region Properties

        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the DateTime (UTC) when the data arrived from the server.
        /// </summary>
        public DateTime DestinationDateTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a warning of an impending leap second to be inserted/deleted in the last minute of the current day.
        /// </summary>
        public LeapIndicator LeapIndicator
        {
            get { return (LeapIndicator)LeapIndicatorValue; }
            set { LeapIndicatorValue = (byte)value; }
        }

        private byte LeapIndicatorValue
        {
            get { return (byte)((Data[0] & LeapIndicatorMask) >> LeapIndicatorOffset); }
            set { Data[0] = (byte)((Data[0] & LeapIndicatorOffset) | value); }
        }

        /// <summary>
        /// Gets the number of bytes in the packet.
        /// </summary>
        public int Length
        {
            get { return Data.Length; }
        }

        /// <summary>
        /// Gets the difference in seconds between the local time and the time retrieved from the server.
        /// </summary>
        public double LocalClockOffset
        {
            get
            {
                return ((double)((ReceiveDateTime.Ticks - OriginateDateTime.Ticks) +
                    (TransmitDateTime.Ticks - DestinationDateTime.Ticks)) / 2) / TicksPerSecond;
            }
        }

        /// <summary>
        /// Gets the operating mode of whatever last altered the packet.
        /// </summary>
        public Mode Mode
        {
            get { return (Mode)ModeValue; }
            set { ModeValue = (byte)value; }
        }

        private byte ModeValue
        {
            get { return (byte)(Data[0] & ModeMask); }
            set { Data[0] = (byte)((Data[0] & ModeComplementMask) | value); }

        }

        /// <summary>
        /// Gets the DateTime (UTC) at which the request departed the client for the server.
        /// </summary>
        public DateTime OriginateDateTime
        {
            get { return TimestampToDateTime(originateIndex); }
            set { DateTimeToTimestamp(value, originateIndex); }
        }

        /// <summary>
        /// Gets the maximum interval between successive messages, in seconds.
        /// </summary>
        public double PollInterval
        {
            get { return Math.Pow(2, (sbyte)Data[2]); }
            set { Data[2] = (byte)Math.Log(value); }
        }

        /// <summary>
        /// Gets the precision of the clock, in seconds.
        /// </summary>
        public double Precision
        {
            get { return Math.Pow(2, (sbyte)Data[3]); }
            set { Data[3] = (byte)Math.Log(value); }
        }

        /// <summary>
        /// Gets the DateTime (UTC) at which the request arrived at the server.
        /// </summary>
        public DateTime ReceiveDateTime
        {
            get { return TimestampToDateTime(receiveIndex); }
            set { DateTimeToTimestamp(value, receiveIndex); }
        }

        /// <summary>
        /// Gets the DateTime (UTC) at which the clock was last set or corrected.
        /// </summary>
        public DateTime ReferenceDateTime
        {
            get { return TimestampToDateTime(referenceIndex); }
            set { DateTimeToTimestamp(value, referenceIndex); }
        }

        /// <summary>
        /// Gets the identifier of the reference source.
        /// </summary>
        public string ReferenceIdentifier
        {
            get
            {
                string result = null;
                switch (Stratum)
                {
                    case Stratum.Unspecified:
                    case Stratum.Primary:
                        UInt32 id = 0;
                        for (int i = 0; i <= 3; i++)
                            id = (id << 8) | Data[referenceIdentifierOffset + i];
                        //if (!RefererenceIdentifierDictionary.TryGetValue(((ReferenceIdentifier)id), out result))
                        {
                            result =
                                    Data[referenceIdentifierOffset].ToString() +
                                    Data[referenceIdentifierOffset + 1] +
                                    Data[referenceIdentifierOffset + 2] +
                                    Data[referenceIdentifierOffset + 3];
                        }
                        break;
                    case Stratum.Secondary:
                    case Stratum.Secondary3:
                    case Stratum.Secondary4:
                    case Stratum.Secondary5:
                    case Stratum.Secondary6:
                    case Stratum.Secondary7:
                    case Stratum.Secondary8:
                    case Stratum.Secondary9:
                    case Stratum.Secondary10:
                    case Stratum.Secondary11:
                    case Stratum.Secondary12:
                    case Stratum.Secondary13:
                    case Stratum.Secondary14:
                    case Stratum.Secondary15:
                        switch (VersionNumber)
                        {
                            case VersionNumber.Version3:
                                result =
                                    Data[referenceIdentifierOffset] + "." +
                                    Data[referenceIdentifierOffset + 1] + "." +
                                    Data[referenceIdentifierOffset + 2] + "." +
                                    Data[referenceIdentifierOffset + 3];
                                break;
                            // The code below works with the Version 4 spec, but many servers respond as v4 but fill this as v3.
                            case VersionNumber.Version4:
                                // result = Timestamp32ToDateTime(referenceIdentifierOffset).ToString();
                                break;
                            default:
                                if (VersionNumber < VersionNumber.Version3)
                                {
                                    result =
                                        Data[referenceIdentifierOffset] + "." +
                                        Data[referenceIdentifierOffset + 1] + "." +
                                        Data[referenceIdentifierOffset + 2] + "." +
                                        Data[referenceIdentifierOffset + 3];
                                }
                                else
                                {
                                    // For future
                                }
                                break;
                        }
                        break;
                    default:
                        break;
                }
                return result;
            }
            set
            {
                switch (Stratum)
                {
                    case Stratum.Unspecified:
                    case Stratum.Primary:
                        var arr = value.ToCharArray();

                        //Data[referenceIdentifierOffset] = arr.Length > 0
                        //                                      ? Convert.ToByte(arr[0].ToString())
                        //                                      : Convert.ToByte("");

                        //Data[referenceIdentifierOffset + 1] = arr.Length > 1
                        //                                      ? Convert.ToByte(arr[1].ToString())
                        //                                      : Convert.ToByte("");

                        //Data[referenceIdentifierOffset + 2] = arr.Length > 2
                        //                                      ? Convert.ToByte(arr[2].ToString())
                        //                                      : Convert.ToByte("");

                        //Data[referenceIdentifierOffset + 3] = arr.Length > 3
                        //                                      ? Convert.ToByte(arr[3].ToString())
                        //                                      : Convert.ToByte("");

                        break;
                    /* case Stratum.Secondary:
                     case Stratum.Secondary3:
                     case Stratum.Secondary4:
                     case Stratum.Secondary5:
                     case Stratum.Secondary6:
                     case Stratum.Secondary7:
                     case Stratum.Secondary8:
                     case Stratum.Secondary9:
                     case Stratum.Secondary10:
                     case Stratum.Secondary11:
                     case Stratum.Secondary12:
                     case Stratum.Secondary13:
                     case Stratum.Secondary14:
                     case Stratum.Secondary15:
                         switch (VersionNumber)
                         {
                             case VersionNumber.Version3:
                                 result = string.Format("{0}.{1}.{2}.{3}",
                                     data[referenceIdentifierOffset],
                                     data[referenceIdentifierOffset + 1],
                                     data[referenceIdentifierOffset + 2],
                                     data[referenceIdentifierOffset + 3]);
                                 break;
                             // The code below works with the Version 4 spec, but many servers respond as v4 but fill this as v3.
                             case VersionNumber.Version4:
                                 // result = Timestamp32ToDateTime(referenceIdentifierOffset).ToString();
                                 break;
                             default:
                                 if (VersionNumber < VersionNumber.Version3)
                                 {
                                     result = string.Format("{0}.{1}.{2}.{3}",
                                     data[referenceIdentifierOffset],
                                     data[referenceIdentifierOffset + 1],
                                     data[referenceIdentifierOffset + 2],
                                     data[referenceIdentifierOffset + 3]);
                                 }
                                 else
                                 {
                                     // For future
                                 }
                                 break;
                         }
                         break;
                     default:
                         break;
                         */
                }
            }
        }

        /// <summary>
        /// Gets the total delay to the primary reference source, in seconds.
        /// </summary>
        public double RootDelay
        {
            get { return SecondsStampToSeconds(4); }
        }

        /// <summary>
        /// Gets the nominal error relative to the primary reference source, in seconds.
        /// </summary>
        public double RootDispersion
        {
            get { return SecondsStampToSeconds(8); }
        }

        /// <summary>
        /// Gets the total roundtrip delay, in seconds.
        /// </summary>
        public double RoundTripDelay
        {
            get
            {
                return (double)((DestinationDateTime.Ticks - OriginateDateTime.Ticks)
                    - (ReceiveDateTime.Ticks - TransmitDateTime.Ticks)) / TicksPerSecond;
            }
        }

        /// <summary>
        /// Gets the stratum level of the clock.
        /// </summary>
        public Stratum Stratum
        {
            get { return (Stratum)StratumValue; }
            set { StratumValue = (byte)value; }
        }

        private byte StratumValue
        {
            get { return Data[1]; }
            set { Data[1] = value; }
        }

        /// <summary>
        /// Gets the DateTime (UTC) at which the reply departed the server for the client.
        /// </summary>
        public DateTime TransmitDateTime
        {
            get { return TimestampToDateTime(transmitIndex); }
            set { DateTimeToTimestamp(value, transmitIndex); }
        }

        /// <summary>
        /// Gets the NTP/SNTP version number.
        /// </summary>
        public VersionNumber VersionNumber
        {
            get { return (VersionNumber)VersionNumberValue; }
            set { VersionNumberValue = (byte)value; }
        }

        private byte VersionNumberValue
        {
            get { return (byte)((Data[0] & VersionNumberMask) >> VersionNumberOffset); }
            set { Data[0] = (byte)((Data[0] & VersionNumberComplementMask) | (value << VersionNumberOffset)); }
        }

        #endregion Properties

        #region Methods

        // Private Methods 

        /// <summary>
        /// Converts a DateTime into a byte array and stores it starting at the position specifed.
        /// </summary>
        /// <param name="dateTime">The DateTime to convert.</param>
        /// <param name="startIndex">The index in the data at which to start.</param>
        private void DateTimeToTimestamp(DateTime dateTime, int startIndex)
        {
            UInt64 ticks = (UInt64)(dateTime - Epoch).Ticks;
            UInt64 seconds = ticks / TicksPerSecond;
            UInt64 fractions = ((ticks % TicksPerSecond) * 0x100000000L) / TicksPerSecond;
            for (int i = 3; i >= 0; i--)
            {
                Data[startIndex + i] = (byte)seconds;
                seconds = seconds >> 8;
            }
            for (int i = 7; i >= 4; i--)
            {
                Data[startIndex + i] = (byte)fractions;
                fractions = fractions >> 8;
            }
        }

        /// <summary>
        /// Converts a 32bit seconds (16 integer part, 16 fractional part) into a double that represents the value in seconds.
        /// </summary>
        /// <param name="startIndex">The index in the data at which to start.</param>
        /// <returns>A double that represents the value in seconds</returns>
        private double SecondsStampToSeconds(int startIndex)
        {
            UInt64 seconds = 0;
            for (int i = 0; i <= 1; i++)
                seconds = (seconds << 8) | Data[startIndex + i];
            UInt64 fractions = 0;
            for (int i = 2; i <= 3; i++)
                fractions = (fractions << 8) | Data[startIndex + i];
            UInt64 ticks = (seconds * TicksPerSecond) + ((fractions * TicksPerSecond) / 0x10000L);
            return (double)ticks / TicksPerSecond;
        }

        private DateTime Timestamp32ToDateTime(int startIndex)
        {
            UInt64 seconds = 0;
            for (int i = 0; i <= 3; i++)
                seconds = (seconds << 8) | Data[startIndex + i];
            UInt64 ticks = (seconds * TicksPerSecond);
            return Epoch + TimeSpan.FromTicks((Int64)ticks);
        }

        /// <summary>
        /// Converts a byte array starting at the position specified into a DateTime.
        /// </summary>
        /// <param name="startIndex">The index in the data at which to start.</param>
        /// <returns>A DateTime converted from a byte array starting at the position specified.</returns>
        private DateTime TimestampToDateTime(int startIndex)
        {
            UInt64 seconds = 0;
            for (int i = 0; i <= 3; i++)
                seconds = (seconds << 8) | Data[startIndex + i];
            UInt64 fractions = 0;
            for (int i = 4; i <= 7; i++)
                fractions = (fractions << 8) | Data[startIndex + i];
            UInt64 ticks = (seconds * TicksPerSecond) + ((fractions * TicksPerSecond) / 0x100000000L);
            return Epoch + TimeSpan.FromTicks((Int64)ticks);
        }
        // Internal Methods 

        /// <summary>
        /// A SNTPData that is used by a client to send to a server to request data.
        /// </summary>
        internal static SNTPData GetClientRequestPacket(VersionNumber versionNumber)
        {
            SNTPData packet = new SNTPData();
            packet.Mode = Mode.Client;
            packet.VersionNumber = versionNumber;
            packet.TransmitDateTime = DateTime.Now.ToUniversalTime();
            return packet;
        }

        #endregion Methods

        #region Conversion Operators

        public static implicit operator SNTPData(byte[] byteArray)
        {
            return new SNTPData(byteArray);
        }

        public static implicit operator byte[](SNTPData sntpPacket)
        {
            return sntpPacket.Data;
        }

        #endregion Conversion Operators

    }
}
