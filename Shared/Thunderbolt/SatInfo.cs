using System;
using Microsoft.SPOT;

namespace TrimbleMonitor.Thunderbolt
{
    class SatInfo
    {
        public SatInfo()
        {
        }

        #region Report Packet 0x49 (Almanac Health Page Report)
        /// <summary>
        /// Report Packet 0x49 provides health information on 32 satellites. Packet data consists of 32
        /// bytes each containing the 6-bit health from almanac page 25. Byte #0 is for satellite #1,
        /// and so on. The receiver sends this packet in response to Command Packet 0x29 and automatically 
        /// when this data is received from a satellite.
        /// </summary>

        private bool health_flag = false;        // packet 49

        public bool IsHealthy
        {
            get { return health_flag; }
            set { health_flag = value; }
        }

        #endregion

        #region Report Packet 0x59 (Satellite Attribute Database Status Report)

        private bool disabled = false;
        private bool forced_healthy = false;

        /// <summary>
        /// Disabled satellites are not used, even when the satellite is in good health. This attribute
        /// identifies any satellites manually disabled by user. The factory default setting is to 
        /// enable all satellites for inclusion in position solution computations if they are in
        /// good health and conform with the mask values for elevation angle, signal level, PDOP, and PDOP Switch
        /// </summary>
        public bool Disabled
        {
            get { return disabled; }
            set { disabled = value; }
        }

        /// <summary>
        /// Satellites with this attribute set indicate that the satellite can be used in the position 
        /// solution, regardless of whether the satellite is in good or bad health. The factory default
        /// setting is to heed satellite health when choosing the satellites includedin a position 
        /// solution computation.
        /// </summary>
        public bool ForcedHealthy
        {
            get { return forced_healthy; }
            set { forced_healthy = value; }
        }
        #endregion

        #region Report Packets 0x47,0x5A,0x5C (General Satellite Info)

        private float sample_len = 0;        // packet 5A

        public float SampleLength
        {
            get { return sample_len; }
            set { sample_len = value; }
        }
        private float sig_level = 0;         // (also packet 47, 5C)

        public float SignalLevel
        {
            get { return sig_level; }
            set { sig_level = value; }
        }
        private float code_phase = 0;

        public float CodePhase
        {
            get { return code_phase; }
            set { code_phase = value; }
        }
        private float doppler = 0;

        public float Doppler
        {
            get { return doppler; }
            set { doppler = value; }
        }
        private double raw_time = 0;

        public double RawTime
        {
            get { return raw_time; }
            set { raw_time = value; }
        }

        #endregion

        #region Report Packet 0x5B (Satellite Ephemeris Status Report)

        private float collection_time = 0;
        /// <summary>
        /// GPS time (in secs) when Ephemeris data is collected from the satellite
        /// </summary>
        public float CollectionTime
        {
            get { return collection_time; }
            set { collection_time = value; }
        }
        private byte eph_health = 0;
        /// <summary>
        /// The 6-bit ephemeris health
        /// </summary>
        public byte EphemerisHealth
        {
            get { return eph_health; }
            set { eph_health = value; }
        }
        private byte iode = 0;

        /// <summary>
        /// Issue of Data Ephemeris. (See the U.S. Government document ICD-GPS-200)
        /// </summary>
        public byte IODE
        {
            get { return iode; }
            set { iode = value; }
        }
        private float toe = 0;

        /// <summary>
        /// Toe in secs, (See the U.S. Government document ICD-GPS-200)
        /// </summary>
        public float Toe
        {
            get { return toe; }
            set { toe = value; }
        }
        private byte fit_interval_flag = 0;

        /// <summary>
        /// See the U.S. Government document ICD-GPS-200
        /// </summary>
        public byte FitIntervalFlag
        {
            get { return fit_interval_flag; }
            set { fit_interval_flag = value; }
        }
        private float user_range_accuracy = 0;

        /// <summary>
        /// User Range Accuracy of satellite, converted to meters from the 4-bit code 
        /// described in ICD-GPS-200
        /// </summary>
        public float UserRangeAccuracy
        {
            get { return user_range_accuracy; }
            set { user_range_accuracy = value; }
        }

        #endregion

        #region Report Packet 0x5C (Satellite Tracking Status Report)

        private byte slot = 0;

        /// <summary>
        /// Internal code assigned to the hardware slot used to track the specified satellite. 
        /// Slot encoding is generally not used in modern receviers. 
        /// </summary>
        public byte Slot
        {
            get { return slot; }
            set { slot = value; }
        }
        private byte chan = 0;

        /// <summary>
        /// Internal code assigned to the hardware channel used to track the specified satellite. For
        /// parallel tracking receivers (which includes most modern receivers), no sequencing of satellites is
        /// done and only one satellite is assigned to a hardware channel. 
        /// </summary>
        public byte Channel
        {
            get { return chan; }
            set { chan = value; }
        }
        private byte acq_flag = 0;

        /// <summary>
        /// Signal acquisition (lock) state of the satellite:
        /// 0 Never acquired
        /// 1 Acquired
        /// 2 Re-opened search
        /// 3 Ephemeris Flag BYTE flag Status of Ephemeris received
        /// </summary>
        public byte AcquisitionFlag
        {
            get { return acq_flag; }
            set { acq_flag = value; }
        }
        private byte eph_flag = 0;

        /// <summary>
        /// Status of Ephemeris received from specified satellite:
        /// 0 Ephemeris is not received from satellite
        /// 
        /// non-zero value:
        /// Good ephemeris received from satellite (less than 4 hours old, good health). 
        /// Note that some receivers use a value of 33 to indicate that the received ephemeris 
        /// was not healthy.
        /// </summary>
        public byte EphemerisFlag
        {
            get { return eph_flag; }
            set { eph_flag = value; }
        }
        private float time_of_week = 0;

        /// <summary>
        /// GPS Time of Last Measurement:
        /// less than 0: No measurements taken
        /// greater or equal to 0: Center of last measurement dwell taken from this satellite
        /// </summary>
        public float TimeOfWeek
        {
            get { return time_of_week; }
            set { time_of_week = value; }
        }
        private float azimuth = 0;

        /// <summary>
        /// Approximate Azimuth angle of satellite (in radians)
        /// </summary>
        public float Azimuth
        {
            get { return azimuth; }
            set { azimuth = value; }
        }
        private float elevation = 0;

        /// <summary>
        /// Approximate Elevation angle of satellite (in radians)
        /// </summary>
        public float Elevation
        {
            get { return elevation; }
            set { elevation = value; }
        }
        private byte age = 0;

        /// <summary>
        /// 0 Flag not set, measurement is new
        /// other Measurement too old to be considered for position solutions
        /// </summary>
        public byte Age
        {
            get { return age; }
            set { age = value; }
        }
        private byte msec = 0;

        /// <summary>
        /// Status of the integer millisecond range to the specified satellite:
        /// 0 Unknown
        /// 1 Acquired from sub-frame data collection
        /// 2 Verified by a bit crossing time
        /// 3 Verified by a successful position fix
        /// 4 Suspected msec error
        /// </summary>
        public byte MsecStatus
        {
            get { return msec; }
            set { msec = value; }
        }
        private byte bad_flag = 0;

        /// <summary>
        /// Current health status of the data:
        /// 0 Data presumed good
        /// 1 Bad parity
        /// 2 Bad ephemeris health
        /// </summary>
        public byte BadDataFlag
        {
            get { return bad_flag; }
            set { bad_flag = value; }
        }
        private byte collecting = 0;

        /// <summary>
        /// Receiver is collecting data from satellite:
        /// 0 Not collecting data
        /// non-zero Collecting data
        /// </summary>
        public byte CollectingData
        {
            get { return collecting; }
            set { collecting = value; }
        }
        #endregion

        #region Report Packet 0x6D (All In-View Satellite Selection Report)

        private bool tracked = false;            // 6D
        private bool used_in_fix = false;

        /// <summary>
        /// True if this satellite is being used by the receiver to determine the current fix.
        /// </summary>
        public bool UsedInFix
        {
            get { return used_in_fix; }
            set { used_in_fix = value; }
        }

        /// <summary>
        /// True if this satellite is being tracked by the receiver.
        /// </summary>
        public bool Tracked
        {
            get { return tracked; }
            set { tracked = value; }
        }

        #endregion

        #region Report Packet 0x8F.A7 (Satellite Solutions)

        private float sat_bias = 0;          // 8F.A7

        public float SatBias
        {
            get { return sat_bias; }
            set { sat_bias = value; }
        }
        private float time_of_fix = 0;

        public float TimeOfFix
        {
            get { return time_of_fix; }
            set { time_of_fix = value; }
        }
        private bool last_bias_msg = false;     // flag set if sat info was from last message

        public bool HasBiasInfo
        {
            get { return last_bias_msg; }
            set { last_bias_msg = value; }
        }

        #endregion
    }
}
