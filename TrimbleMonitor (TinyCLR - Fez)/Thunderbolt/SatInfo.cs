using System;
using System.Text;

namespace TrimbleMonitor.Thunderbolt
{
    class SatInfo
    {
        public SatInfo()
        {
            HasBiasInfo = false;
            TimeOfFix = 0;
            SatBias = 0;
            Tracked = false;
            UsedInFix = false;
            CollectingData = 0;
            BadDataFlag = 0;
            MsecStatus = 0;
            Age = 0;
            Elevation = 0;
            Azimuth = 0;
            TimeOfWeek = 0;
            EphemerisFlag = 0;
            AcquisitionFlag = 0;
            Channel = 0;
            Slot = 0;
            UserRangeAccuracy = 0;
            FitIntervalFlag = 0;
            Toe = 0;
            IODE = 0;
            EphemerisHealth = 0;
            CollectionTime = 0;
            RawTime = 0;
            Doppler = 0;
            CodePhase = 0;
            SignalLevel = 0;
            SampleLength = 0;
            ForcedHealthy = false;
            Disabled = false;
        }

        #region Report Packet 0x49 (Almanac Health Page Report)
        /// <summary>
        /// Report Packet 0x49 provides health information on 32 satellites. Packet data consists of 32
        /// bytes each containing the 6-bit health from almanac page 25. Byte #0 is for satellite #1,
        /// and so on. The receiver sends this packet in response to Command Packet 0x29 and automatically 
        /// when this data is received from a satellite.
        /// </summary>

        public bool IsHealthy { get; set; } = false;

        #endregion

        #region Report Packet 0x59 (Satellite Attribute Database Status Report)

        /// <summary>
        /// Disabled satellites are not used, even when the satellite is in good health. This attribute
        /// identifies any satellites manually disabled by user. The factory default setting is to 
        /// enable all satellites for inclusion in position solution computations if they are in
        /// good health and conform with the mask values for elevation angle, signal level, PDOP, and PDOP Switch
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Satellites with this attribute set indicate that the satellite can be used in the position 
        /// solution, regardless of whether the satellite is in good or bad health. The factory default
        /// setting is to heed satellite health when choosing the satellites includedin a position 
        /// solution computation.
        /// </summary>
        public bool ForcedHealthy { get; set; }

        #endregion

        #region Report Packets 0x47,0x5A,0x5C (General Satellite Info)

        public float SampleLength { get; set; }

        public float SignalLevel { get; set; }

        public float CodePhase { get; set; }

        public float Doppler { get; set; }

        public double RawTime { get; set; }

        #endregion

        #region Report Packet 0x5B (Satellite Ephemeris Status Report)

        /// <summary>
        /// GPS time (in secs) when Ephemeris data is collected from the satellite
        /// </summary>
        public float CollectionTime { get; set; }

        /// <summary>
        /// The 6-bit ephemeris health
        /// </summary>
        public byte EphemerisHealth { get; set; }

        /// <summary>
        /// Issue of Data Ephemeris. (See the U.S. Government document ICD-GPS-200)
        /// </summary>
        public byte IODE { get; set; }

        /// <summary>
        /// Toe in secs, (See the U.S. Government document ICD-GPS-200)
        /// </summary>
        public float Toe { get; set; }

        /// <summary>
        /// See the U.S. Government document ICD-GPS-200
        /// </summary>
        public byte FitIntervalFlag { get; set; }

        /// <summary>
        /// User Range Accuracy of satellite, converted to meters from the 4-bit code 
        /// described in ICD-GPS-200
        /// </summary>
        public float UserRangeAccuracy { get; set; }

        #endregion

        #region Report Packet 0x5C (Satellite Tracking Status Report)

        /// <summary>
        /// Internal code assigned to the hardware slot used to track the specified satellite. 
        /// Slot encoding is generally not used in modern receivers. 
        /// </summary>
        public byte Slot { get; set; }

        /// <summary>
        /// Internal code assigned to the hardware channel used to track the specified satellite. For
        /// parallel tracking receivers (which includes most modern receivers), no sequencing of satellites is
        /// done and only one satellite is assigned to a hardware channel. 
        /// </summary>
        public byte Channel { get; set; }

        /// <summary>
        /// Signal acquisition (lock) state of the satellite:
        /// 0 Never acquired
        /// 1 Acquired
        /// 2 Re-opened search
        /// 3 Ephemeris Flag BYTE flag Status of Ephemeris received
        /// </summary>
        public byte AcquisitionFlag { get; set; }

        /// <summary>
        /// Status of Ephemeris received from specified satellite:
        /// 0 Ephemeris is not received from satellite
        /// 
        /// non-zero value:
        /// Good ephemeris received from satellite (less than 4 hours old, good health). 
        /// Note that some receivers use a value of 33 to indicate that the received ephemeris 
        /// was not healthy.
        /// </summary>
        public byte EphemerisFlag { get; set; }

        /// <summary>
        /// GPS Time of Last Measurement:
        /// less than 0: No measurements taken
        /// greater or equal to 0: Center of last measurement dwell taken from this satellite
        /// </summary>
        public float TimeOfWeek { get; set; }

        /// <summary>
        /// Approximate Azimuth angle of satellite (in radians)
        /// </summary>
        public float Azimuth { get; set; }

        /// <summary>
        /// Approximate Elevation angle of satellite (in radians)
        /// </summary>
        public float Elevation { get; set; }

        /// <summary>
        /// 0 Flag not set, measurement is new
        /// other Measurement too old to be considered for position solutions
        /// </summary>
        public byte Age { get; set; }

        /// <summary>
        /// Status of the integer millisecond range to the specified satellite:
        /// 0 Unknown
        /// 1 Acquired from sub-frame data collection
        /// 2 Verified by a bit crossing time
        /// 3 Verified by a successful position fix
        /// 4 Suspected msec error
        /// </summary>
        public byte MsecStatus { get; set; }

        /// <summary>
        /// Current health status of the data:
        /// 0 Data presumed good
        /// 1 Bad parity
        /// 2 Bad ephemeris health
        /// </summary>
        public byte BadDataFlag { get; set; }

        /// <summary>
        /// Receiver is collecting data from satellite:
        /// 0 Not collecting data
        /// non-zero Collecting data
        /// </summary>
        public byte CollectingData { get; set; }

        #endregion

        #region Report Packet 0x6D (All In-View Satellite Selection Report)

        /// <summary>
        /// True if this satellite is being used by the receiver to determine the current fix.
        /// </summary>
        public bool UsedInFix { get; set; }

        /// <summary>
        /// True if this satellite is being tracked by the receiver.
        /// </summary>
        public bool Tracked { get; set; }

        #endregion

        #region Report Packet 0x8F.A7 (Satellite Solutions)

        public float SatBias { get; set; }

        public float TimeOfFix { get; set; }

        public bool HasBiasInfo { get; set; }

        #endregion

        public long MillisAtLastUpdate { get; set; } = 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=========");
            sb.AppendLine("HasBiasInfo: " + HasBiasInfo);
            sb.AppendLine("TimeOfFix: " + TimeOfFix);
            sb.AppendLine("SatBias: " + SatBias);
            sb.AppendLine("Tracked: " + Tracked);
            sb.AppendLine("UsedInFix: " + UsedInFix);
            sb.AppendLine("CollectingData: " + CollectingData);
            sb.AppendLine("BadDataFlag: " + BadDataFlag);
            sb.AppendLine("MsecStatus: " + MsecStatus);
            sb.AppendLine("Age: " + Age);
            sb.AppendLine("Elevation: " + Elevation);
            sb.AppendLine("Azimuth: " + Azimuth);
            sb.AppendLine("TimeOfWeek: " + TimeOfWeek);
            sb.AppendLine("EphemerisFlag: " + EphemerisFlag);
            sb.AppendLine("AcquisitionFlag: " + AcquisitionFlag);
            sb.AppendLine("Channel: " + Channel);
            sb.AppendLine("Slot: " + Slot);
            sb.AppendLine("UserRangeAccuracy: " + UserRangeAccuracy);
            sb.AppendLine("FitIntervalFlag: " + FitIntervalFlag);
            sb.AppendLine("Toe: " + Toe);
            sb.AppendLine("IODE: " + IODE);
            sb.AppendLine("EphemerisHealth: " + EphemerisHealth);
            sb.AppendLine("CollectionTime: " + CollectionTime);
            sb.AppendLine("RawTime: " + RawTime);
            sb.AppendLine("Doppler: " + Doppler);
            sb.AppendLine("CodePhase: " + CodePhase);
            sb.AppendLine("SignalLevel: " + SignalLevel);
            sb.AppendLine("SampleLength: " + SampleLength);
            sb.AppendLine("ForcedHealthy: " + ForcedHealthy);
            sb.AppendLine("Disabled: " + Disabled);
            sb.AppendLine("MillisAtLastUpdate: " + MillisAtLastUpdate);
            sb.AppendLine("MillisSinceLastUpdate: " + (DateTime.UtcNow.Ticks / 10000 - MillisAtLastUpdate));
            sb.AppendLine("=========");
            return sb.ToString();
        }
    }
}
