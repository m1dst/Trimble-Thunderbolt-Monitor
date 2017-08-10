using System;
using Microsoft.SPOT;
using System.Reflection;

namespace TrimbleMonitor.Thunderbolt
{

    public enum CriticalAlarms
    {
        NoCriticalAlarms,
        ROMchecksumError,
        RAMcheckhasfailed,
        Powersupplyfailure,
        FPGAcheckhasfailed,
        Oscillatorcontrolvoltageatrail
    }

    public enum MinorAlarms
    {
        NoMinorAlarms,
        ControlVoltageNearRail,
        AntennaOpen,
        AntennaShorted,
        NotTrackingSatellites,
        DoingSurvey,
        NoStoredPosition,
        LeapSecondPending,
        InTestMode,
        AlmanacBeingUpdated
    }

    /// <summary>
    /// Display mode for time (GPS or UTC)
    /// </summary>
    public enum TimingModes : byte
    {
        GPS,
        UTC
    }

    /// <summary>
    /// Time Status
    /// </summary>
    public enum TimeType : byte
    {
        NoTimeAvailable,
        NoUTCOffset,
        UserSetTime,
        UTCTimeOk,
        GPSTimeOk
    }

    public enum FixMode : byte
    {
        Auto,
        Manual
    }

    /// <summary>
    /// Values that describe the demension of the current fix
    /// </summary>
    public enum FixDimension : byte
    {
        None,
        Clock_1D,
        Position_2D,
        Position_3D,
        OverDetermined
    }

    /// <summary>
    /// 1	    Ideal	    This is the highest possible confidence level to be used for applications 
    ///                     demanding the highest possible precision at all times.
    /// 1-2	    Excellent	At this confidence level, positional measurements are considered accurate 
    ///                     enough to meet all but the most sensitive applications.
    /// 2-5	    Good	    Represents a level that marks the minimum appropriate for making business 
    ///                     decisions. Positional measurements could be used to make reliable in-route
    ///                     navigation suggestions to the user.
    /// 5-10    Moderate	Positional measurements could be used for calculations, but the fix quality 
    ///                     could still be improved. A more open view of the sky is recommended.
    /// 10-20	Fair	    Represents a low confidence level. Positional measurements should be discarded
    ///                     or used only to indicate a very rough estimate of the current location.
    /// >20	    Poor	    At this level, measurements are inaccurate by as much as 300 meters with a 
    ///                     6 meter accurate device (50 DOP × 6 meters) and should be discarded.
    /// </summary>
    public enum FixPrecision : byte
    {
        Ideal,
        Excellent,
        Good,
        Moderate,
        Fair,
        Poor
    }

    public enum ReceiverStatus : byte
    {
        DoingFixes = 0x00,
        NoGPSTime = 0x01,
        Reserved = 0x02,
        PDOPTooHigh = 0x03,
        NoUsableSats = 0x08,
        Only1Sat = 0x09,
        Only2Sats = 0x0A,
        Only3Sats = 0x0B,
        SatUnusable = 0x0C, // This message is included only when the one-satellite mode is in effect and 
        // a specific satellite is chosen with Command Packet 0x34, 
        // the selected satellite is not usable.
        TAIMRejected = 0x10,
        Unknown = 0xFF
    }

    public enum ReceiverMode : byte
    {
        Automatic = 0,
        SingleSatellite = 1,
        Horizontal = 3,
        FullPosition = 4,
        OverDeterminedClock = 7,
        Unknown
    }

    public enum DiscipliningMode : byte
    {
        Normal = 0,
        PowerUp = 1,
        AutoHoldover = 2,
        ManualHoldover = 3,
        Recovery = 4,
        NotUsed = 5,
        Disabled = 6,
        Unknown
    }

    public enum DiscipliningActivity : byte
    {
        PhaseLocking = 0,
        OscillatorWarmUp = 1,
        FrequencyLocking = 2,
        PlacingPPS = 3,
        InitializingLoopFilter = 4,
        CompensatingOCXO = 5,
        Inactive = 6,
        NotUsed = 7,
        RecoveryMode = 8,
        CalibrationVoltage = 9,
        Unknown = 0xFF
    }

    public enum Dynamics : byte
    {
        Land = 1,
        Sea = 2,
        Air = 3,
        Stationary = 4
    }

    public enum Foilage : byte
    {
        Never = 0,
        Sometimes = 1,
        Always = 2
    }

}

