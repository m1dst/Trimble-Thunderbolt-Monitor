using System;
using Microsoft.SPOT;
using System.Reflection;

namespace TrimbleMonitor.Thunderbolt
{

    public static class EnumerationStrings
    {
        public static string CriticalAlarmsString(CriticalAlarms value)
        {
            switch (value)
            {
                case CriticalAlarms.NoCriticalAlarms:
                    return "No critical alarms";
                case CriticalAlarms.ROMchecksumError:
                    return "ROM checksum error";
                case CriticalAlarms.RAMcheckhasfailed:
                    return "RAM check has failed";
                case CriticalAlarms.Powersupplyfailure:
                    return "Power supply failure";
                case CriticalAlarms.FPGAcheckhasfailed:
                    return "FPGA check failed";
                case CriticalAlarms.Oscillatorcontrolvoltageatrail:
                    return "Ctrl voltage at rail";
                default:
                    return "Unknown";
            }
        }

        public static string MinorAlarmsString(MinorAlarms value)
        {
            switch (value)
            {
                case MinorAlarms.NoMinorAlarms:
                    return "No minor alarms";
                case MinorAlarms.ControlVoltageNearRail:
                    return "Ctrl voltage nr rail";
                case MinorAlarms.AntennaOpen:
                    return "Antenna open";
                case MinorAlarms.AntennaShorted:
                    return "Antenna shorted";
                case MinorAlarms.NotTrackingSatellites:
                    return "Not tracking sats";
                case MinorAlarms.DoingSurvey:
                    return "Survey - in progress";
                case MinorAlarms.NoStoredPosition:
                    return "No stored position";
                case MinorAlarms.LeapSecondPending:
                    return "Leap second pending";
                case MinorAlarms.InTestMode:
                    return "In test mode";
                case MinorAlarms.AlmanacBeingUpdated:
                    return "Almanac updating";
                default:
                    return "Unknown";
            }
        }

        public static string ReceiverStatusString(ReceiverStatus value)
        {
            switch (value)
            {
                case ReceiverStatus.DoingFixes:
                    return "Doing Fixes";
                case ReceiverStatus.NoGPSTime:
                    return "No GPS Time";
                case ReceiverStatus.Reserved:
                    return "Reserved";
                case ReceiverStatus.PDOPTooHigh:
                    return "PDOP Too High";
                case ReceiverStatus.NoUsableSats:
                    return "No Sats Avail";
                case ReceiverStatus.Only1Sat:
                    return "Only One Sat";
                case ReceiverStatus.Only2Sats:
                    return "Only 2 Sats";
                case ReceiverStatus.Only3Sats:
                    return "Only 3 Sats";
                case ReceiverStatus.SatUnusable:
                    return "Sat Unuseable";
                case ReceiverStatus.TAIMRejected:
                    return "TAIM Rejected";
                default:
                    return "Unknown";
            }
        }

        public static string TimeTypeString(TimeType value)
        {
            switch (value)
            {
                case TimeType.NoTimeAvailable:
                    return "No Time Available";
                case TimeType.NoUTCOffset:
                    return "No UTC Offset";
                case TimeType.UserSetTime:
                    return "User Set Time";
                case TimeType.UTCTimeOk:
                    return "UTC Time OK";
                case TimeType.GPSTimeOk:
                    return "GPS Time OK";
                default:
                    return "Unknown";
            }
        }

        public static string FixModeString(FixMode value)
        {
            switch (value)
            {
                case FixMode.Auto:
                    return "Auto";
                case FixMode.Manual:
                    return "Manual";
                default:
                    return "Unknown";
            }
        }

        public static string FixDimensionString(FixDimension value)
        {
            switch (value)
            {
                case FixDimension.None:
                    return "None";
                case FixDimension.Clock_1D:
                    return "1D Clock";
                case FixDimension.Position_2D:
                    return "2D Position";
                case FixDimension.Position_3D:
                    return "3D Position";
                case FixDimension.OverDetermined:
                    return "OverDet Clock";
                default:
                    return "Unknown";
            }
        }

        public static string ReceiverModeString(ReceiverMode value)
        {
            switch (value)
            {
                case ReceiverMode.Automatic:
                    return "Automatic";
                case ReceiverMode.SingleSatellite:
                    return "Single Satellite";
                case ReceiverMode.Horizontal:
                    return "Horizontal";
                case ReceiverMode.FullPosition:
                    return "Full Position";
                case ReceiverMode.OverDeterminedClock:
                    return "OverDet Clock";
                default:
                    return "Unknown";
            }
        }

        public static string DiscipliningModeString(DiscipliningMode value)
        {
            switch (value)
            {
                case DiscipliningMode.Normal:
                    return "Normal";
                case DiscipliningMode.PowerUp:
                    return "Power Up";
                case DiscipliningMode.AutoHoldover:
                    return "Auto Hold";
                case DiscipliningMode.ManualHoldover:
                    return "Manual Hold";
                case DiscipliningMode.Recovery:
                    return "Recovery";
                case DiscipliningMode.NotUsed:
                    return "Not Used";
                case DiscipliningMode.Disabled:
                    return "Disabled";
                default:
                    return "Unknown";
            }
        }

        public static string DiscipliningActivityString(DiscipliningActivity value)
        {
            switch (value)
            {
                case DiscipliningActivity.PhaseLocking:
                    return "Phase Locking";
                case DiscipliningActivity.OscillatorWarmUp:
                    return "Osc Warm Up";
                case DiscipliningActivity.FrequencyLocking:
                    return "Freq Locking";
                case DiscipliningActivity.PlacingPPS:
                    return "Placing PPS";
                case DiscipliningActivity.InitializingLoopFilter:
                    return "Init Loop Fil";
                case DiscipliningActivity.CompensatingOCXO:
                    return "Comp OCXO";
                case DiscipliningActivity.Inactive:
                    return "Inactive";
                case DiscipliningActivity.NotUsed:
                    return "Not Used";
                case DiscipliningActivity.RecoveryMode:
                    return "Recovery Mode";
                case DiscipliningActivity.CalibrationVoltage:
                    return "Calib Voltage";
                default:
                    return "Unknown";
            }
        }

    }


}
