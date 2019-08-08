using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
#if(NETDUINO)
using SecretLabs.NETMF.Hardware.Netduino;
#endif
using MicroLiquidCrystal;
using System.IO.Ports;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using TrimbleMonitor.Thunderbolt;
using Math = System.Math;

namespace TrimbleMonitor
{
    public class Program
    {
        // Percentage you would like the LCD set to.  Between 0 and 100.
        const byte LcdBrightness = 100;

        static DfRobotLcdShield _lcdShield;
        static ThunderBolt _thunderbolt;
#if(NTP)
        static NtpServer _ntpServer;
#endif

        static int _pageNumber = 1;
        static int _previousPageNumber = 1;
#if(NTP)
        static int _numberOfPages = 8;
#else
        static int _numberOfPages = 7;
#endif

        private static int[] _prns = new int[12]; 
        static bool _isSurveyInProgress = false;

#if(NETDUINO)
        static readonly OutputPort MinorLed = new OutputPort(Pins.GPIO_PIN_D12, false);
        static readonly OutputPort MajorLed = new OutputPort(Pins.GPIO_PIN_D11, false);
#endif
#if(FEZLEMUR)
        static readonly OutputPort MinorLed = new OutputPort(GHI.Pins.FEZLemur.Gpio.D12, false);
        static readonly OutputPort MajorLed = new OutputPort(GHI.Pins.FEZLemur.Gpio.D11, false);
#endif

        public static void Main()
        {
            _lcdShield = new DfRobotLcdShield(20, 4);

            // Create a custom degrees symbol (°), store it on the LCD for later use with the lat/long display
            _lcdShield.CreateChar(0, new byte[] { 0x0, 0x4, 0xa, 0x4, 0x0, 0x0, 0x0, 0x0 });

            // 0 bars are not required as that would just require sending " "
            _lcdShield.CreateChar(1, new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1F });
            _lcdShield.CreateChar(2, new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1F, 0x1F });
            _lcdShield.CreateChar(3, new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x1F, 0x1F, 0x1F });
            _lcdShield.CreateChar(4, new byte[] { 0x0, 0x0, 0x0, 0x0, 0x1F, 0x1F, 0x1F, 0x1F });
            _lcdShield.CreateChar(5, new byte[] { 0x0, 0x0, 0x0, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F });
            _lcdShield.CreateChar(6, new byte[] { 0x0, 0x0, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F });
            _lcdShield.CreateChar(7, new byte[] { 0x0, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F });
            // 8 bars are not required as that would just require sending byte 255.

#if(NETDUINO)
            _thunderbolt = new ThunderBolt("COM1", AngleUnits.Degrees, AltitudeUnits.Meters, new OutputPort(Pins.GPIO_PIN_D13, false));
#endif
#if(FEZLEMUR)
            _thunderbolt = new ThunderBolt("COM1", AngleUnits.Degrees, AltitudeUnits.Meters, new OutputPort(GHI.Pins.FEZLemur.Gpio.D13, false));
#endif

#if(NTP)
            _ntpServer = new NtpServer();
#endif

            _lcdShield.OnButtonPressed += LcdshieldOnOnButtonPressed;

            DisplaySplash();

#if(NETDUINO)
            var backlight = new Microsoft.SPOT.Hardware.PWM(PWMChannels.PWM_PIN_D10, 10000, LcdBrightness / 100d, false);
            backlight.Start();
#endif

            TestLeds();

            _thunderbolt.Open();
            _thunderbolt.TimingMode = TimingModes.UTC;
            _thunderbolt.RequestManufacturingParameters();
            _thunderbolt.RequestFirmwareVersion();
            _thunderbolt.SetupUnitForDisciplining();
            _thunderbolt.RequestTrackedSatelliteStatus();

            _thunderbolt.TimeChanged += ThunderboltOnTimeChanged;

            Thread.Sleep(3000);

            DisplayVersion();

#if(NTP)
            _ntpServer.Start();
#endif

            while (true)
            {
                if (_thunderbolt.IsSerialDataBeingReceived)
                {

                    if (!_isSurveyInProgress && _thunderbolt.IsSurveyInProgress())
                    {
                        // Survey has just started.  Jump to the page displaying survey progress.
                        _previousPageNumber = _pageNumber;  // Take note of the page we were on so we can switch back later.
                        _isSurveyInProgress = true;
                        _pageNumber = 2; // Set the new page to jump to.
                    }
                    else if (_isSurveyInProgress && !_thunderbolt.IsSurveyInProgress())
                    {
                        // Survey has just finished.  Jump to the previous page we were displaying.
                        _pageNumber = _previousPageNumber;
                        _isSurveyInProgress = false;
                    }

                    switch (_pageNumber)
                    {
                    case 1:
                        DisplayScreenOne();
                        break;
                    case 2:
                        DisplayScreenTwo();
                        break;
                    case 3:
                        DisplayScreenThree();
                        break;
                    case 4:
                        DisplayScreenFour();
                        break;
                    case 5:
                        DisplaySatelliteSignalScreen();
                        break;
                    case 6:
                        DisplayPRNScreen();
                        break;
                    case 7:
                        DisplayDOPScreen();
                        break;
#if(NTP)
                    case 8:
                        DisplayScreenNTP();
                        break;
#endif
                    default:
                        DisplayScreenOne();
                        break;
                    }

                    UpdateAlarmIndicators();

                }
                else
                {
                    DisplayNoSerialDataScreen();
                }

            }

        }

        private static void ThunderboltOnTimeChanged(object sender, EventArgs eventArgs)
        {
            if (_thunderbolt.IsSerialDataBeingReceived)
            {
                // When we receive a time packet, request the satellite statuses.
                // This is just a simple way to regulate/throttle the requests.
                _thunderbolt.RequestSatelliteList();
                _thunderbolt.RequestTrackedSatelliteStatus();
            }
        }

        private static void TestLeds()
        {
            MinorLed.Write(true);
            MajorLed.Write(true);
            Thread.Sleep(100);
            MinorLed.Write(false);
            MajorLed.Write(false);
            Thread.Sleep(100);
            MinorLed.Write(true);
            MajorLed.Write(false);
            Thread.Sleep(100);
            MinorLed.Write(false);
            MajorLed.Write(false);
            Thread.Sleep(100);
            MinorLed.Write(false);
            MajorLed.Write(true);
            Thread.Sleep(100);
            MinorLed.Write(false);
            MajorLed.Write(false);
            Thread.Sleep(100);
            MinorLed.Write(true);
            MajorLed.Write(true);
            Thread.Sleep(100);
            MinorLed.Write(false);
            MajorLed.Write(false);
        }

        private static void LcdshieldOnOnButtonPressed(object sender, DfRobotLcdShield.Buttons buttonPressed)
        {
            switch (buttonPressed)
            {
                case DfRobotLcdShield.Buttons.Command1:
                    _thunderbolt.ReceiverMode = ReceiverMode.FullPosition;
                    break;

                case DfRobotLcdShield.Buttons.Command2:
                    _thunderbolt.ReceiverMode = ReceiverMode.OverDeterminedClock;
                    break;

                case DfRobotLcdShield.Buttons.Command3:
                    _thunderbolt.set_survey_params(1, 1, 200);
                    _thunderbolt.start_self_survey();
                    break;

                case DfRobotLcdShield.Buttons.Up:
                    _pageNumber++;
                    if (_pageNumber > _numberOfPages) { _pageNumber = 1; }
                    _previousPageNumber = _pageNumber;  // Setting this value to override the survey finished change event.
                    break;

                case DfRobotLcdShield.Buttons.Down:
                    _pageNumber--;
                    if (_pageNumber < 1) { _pageNumber = _numberOfPages; }
                    _previousPageNumber = _pageNumber;  // Setting this value to override the survey finished change event.
                    break;
            }
        }

        static void DisplaySplash()
        {
            _lcdShield.Clear();
            _lcdShield.WriteLine(0, "Trimble Thunderbolt", TextAlign.Centre);
            _lcdShield.WriteLine(1, "Monitor (M1DST)", TextAlign.Centre);
            _lcdShield.WriteLine(2, "www.m1dst.co.uk", TextAlign.Centre);
            _lcdShield.WriteLine(3, "Version 1.1.1", TextAlign.Centre);
        }

        static void DisplayVersion()
        {
            _lcdShield.WriteLine(0, "TBolt FW Ver: " + _thunderbolt.FirmwareVersion.Major + "." + _thunderbolt.FirmwareVersion.Minor);
            _lcdShield.WriteLine(1, "TBolt HW Ver: " + (_thunderbolt.BuildVersion.Major > 0 ? _thunderbolt.BuildVersion.Major + "." + _thunderbolt.BuildVersion.Minor : "N/A"));
            _lcdShield.WriteLine(2, "Built: " + _thunderbolt.BuildVersion.Date.ToString(@"dd-MMM-yyyy"));
            _lcdShield.WriteLine(3, "Serial Num: " + _thunderbolt.BuildVersion.SerialNumber);
            Thread.Sleep(5000);
        }

        static void DisplayNoSerialDataScreen()
        {
            _lcdShield.WriteLine(0, "No RS232 data avail.");
            _lcdShield.WriteLine(1, "Config GPS: 9600 8N1");
            _lcdShield.WriteLine(2, "Data: DB9 pin 2");
            _lcdShield.WriteLine(3, "GND:  DB9 pin 5");
        }

        static void DisplayScreenOne()
        {
            var mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdShield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));
            _lcdShield.WriteLine(1, "  GPS: " + EnumerationStrings.ReceiverStatusString(_thunderbolt.GpsReceiverReceiverStatus));
            _lcdShield.WriteLine(2, "DActv: " + EnumerationStrings.DiscipliningActivityString(_thunderbolt.DisciplineActivity));
            _lcdShield.WriteLine(3, "10MHz: " + (_thunderbolt.OscOffset.ToString("N3") + "ppb ").PadLeft(10) + GetAlarmIndicatorString());
        }

        static void DisplayScreenTwo()
        {
            var mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdShield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));
            _lcdShield.WriteLine(1, " RX M: " + EnumerationStrings.ReceiverModeString(_thunderbolt.ReceiverMode));
            _lcdShield.WriteLine(2, "DscpM: " + EnumerationStrings.DiscipliningModeString(_thunderbolt.DisciplineMode));
            if (_isSurveyInProgress)
            {
                _lcdShield.WriteLine(3, "Survey: " + (_thunderbolt.SurveyProgress + "%").PadLeft(4).PadRight(9) + GetAlarmIndicatorString());
            }
            else
            {
                var uptime = PowerState.Uptime;
                _lcdShield.WriteLine(3, ("Up:" + uptime.Days.ToString().PadLeft(4) + "D:" + uptime.Hours.ToString().PadLeft(2, '0') + "H:" + uptime.Minutes.ToString().PadLeft(2, '0') + "M").PadRight(17) + GetAlarmIndicatorString());
            }

        }

        static void DisplayScreenThree()
        {
            var mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdShield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));

            _lcdShield.SetCursorPosition(0, 1);
            var s = "Lat:" + _thunderbolt.CurrentPosition.Latitude.ToString("N4").PadLeft(9);
            _lcdShield.Write(s);
            _lcdShield.WriteByte(0);
            _lcdShield.Write("".PadLeft(16 - (s.Length + 1)));
            _lcdShield.Write("Alt:");

            _lcdShield.SetCursorPosition(0, 2);
            s = "Lon:" + _thunderbolt.CurrentPosition.Longitude.ToString("N4").PadLeft(9);
            _lcdShield.Write(s);
            _lcdShield.WriteByte(0);
            _lcdShield.Write("".PadLeft(14 - (s.Length + 1)));
            _lcdShield.Write((_thunderbolt.CurrentPosition.Altitude.ToString("N0") + "m").PadLeft(6));

            _lcdShield.WriteLine(3, "Grid: " + MaidenheadLocator.LatLongToLocator(_thunderbolt.CurrentPosition.Latitude, _thunderbolt.CurrentPosition.Longitude).PadRight(11) + GetAlarmIndicatorString());

        }

        static void DisplayScreenFour()
        {
            if (_thunderbolt.CriticalAlarms == 0)
            {
                _lcdShield.WriteLine(0, "No critical alarms");
            }
            else
            {
                if ((_thunderbolt.CriticalAlarms & 0x01) == 0x01)
                {
                    _lcdShield.WriteLine(0, "ROM checksum");
                }
                else if ((_thunderbolt.CriticalAlarms & 0x02) == 0x02)
                {
                    _lcdShield.WriteLine(0, "RAM check");
                }
                else if ((_thunderbolt.CriticalAlarms & 0x04) == 0x04)
                {
                    _lcdShield.WriteLine(0, "PSU fault");
                }
                else if ((_thunderbolt.CriticalAlarms & 0x08) == 0x08)
                {
                    _lcdShield.WriteLine(0, "FPGA check");
                }
                else if ((_thunderbolt.CriticalAlarms & 0x10) == 0x10)
                {
                    _lcdShield.WriteLine(0, "Osc. control voltage");
                }

            }

            if (_thunderbolt.MinorAlarms == 0)
            {
                _lcdShield.WriteLine(1, "No minor alarms");
            }
            else
            {

                if ((_thunderbolt.MinorAlarms & 0x01) == 0x01)
                {
                    _lcdShield.WriteLine(1, "Osc. control voltage");
                }
                else if ((_thunderbolt.MinorAlarms & 0x02) == 0x02)
                {
                    _lcdShield.WriteLine(1, "No antenna connected");
                }
                else if ((_thunderbolt.MinorAlarms & 0x04) == 0x04)
                {
                    _lcdShield.WriteLine(1, "Antenna shorted");
                }
                else if ((_thunderbolt.MinorAlarms & 0x08) == 0x08)
                {
                    _lcdShield.WriteLine(1, "No usable satellites");
                }
                else if ((_thunderbolt.MinorAlarms & 0x10) == 0x10)
                {
                    _lcdShield.WriteLine(1, "Not disciplining");
                }
                else if ((_thunderbolt.MinorAlarms & 0x20) == 0x20)
                {
                    _lcdShield.WriteLine(1, "Survey in progress");
                }
                else if ((_thunderbolt.MinorAlarms & 0x40) == 0x40)
                {
                    _lcdShield.WriteLine(1, "No stored position");
                }
                else if ((_thunderbolt.MinorAlarms & 0x80) == 0x80)
                {
                    _lcdShield.WriteLine(1, "Leap second pending");
                }
                else if ((_thunderbolt.MinorAlarms & 0x100) == 0x100)
                {
                    _lcdShield.WriteLine(1, "In test mode");
                }
                else if ((_thunderbolt.MinorAlarms & 0x200) == 0x200)
                {
                    _lcdShield.WriteLine(1, "Pos. questionable");
                }
                else if ((_thunderbolt.MinorAlarms & 0x400) == 0x400)
                {
                    _lcdShield.WriteLine(1, "EEPROM invalid");
                }
                else if ((_thunderbolt.MinorAlarms & 0x800) == 0x800)
                {
                    _lcdShield.WriteLine(1, "Almanac not complete");
                }
            }

            _lcdShield.WriteLine(2, "DAC: " + _thunderbolt.DacVoltage.ToString("N6") + "V");

            _lcdShield.SetCursorPosition(0, 3);
            _lcdShield.Write("Temp: " + _thunderbolt.Temperature.ToString("N2"));
            _lcdShield.WriteByte(0);
            _lcdShield.Write("C".PadRight(5) + GetAlarmIndicatorString());
        }

        static void DisplaySatelliteSignalScreen()
        {

            var satStatus = "------------".ToCharArray();
            var satSignal = new int[12];

            var numberOfSatsUsedInFix = 0;
            var numberOfSatsTracked = 0;

            foreach (var satellite in _thunderbolt.Satellites)
            {

                //Debug.WriteLine(satellite.ToString());

                // Only display information for tracked satellites.
                if (satellite.Tracked)
                {

                    numberOfSatsTracked++;

                    if (satellite.Disabled)
                    {
                        satStatus[satellite.Channel] = 'D'; // disabled
                    }
                    else if (satellite.UsedInFix)
                    {
                        satStatus[satellite.Channel] = 'F'; // being used for fixes.
                        numberOfSatsUsedInFix++;
                    }
                    else if (satellite.AcquisitionFlag == 1) // acquired
                    {
                        satStatus[satellite.Channel] = 'A';
                    }
                    else if (satellite.AcquisitionFlag == 2) // re-opened search
                    {
                        satStatus[satellite.Channel] = '*';
                    }
                    else
                    {
                        satStatus[satellite.Channel] = 'T';
                    }

                    // Map the signal level to a number between 0 and 8.
                    if (satellite.SignalLevel >= 0)
                    {
                        satSignal[satellite.Channel] = (int)Math.Round(satellite.SignalLevel.Map(0, 50, 0, 8));
                    }

                }

            }

            var mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdShield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));
            _lcdShield.WriteLine(1, ("Status: " + new string(satStatus)).PadRight(20));

            _lcdShield.SetCursorPosition(0, 2);
            _lcdShield.Write("Signal: ");

            foreach (var signal in satSignal)
            {
                //Debug.WriteLine(signal.ToString());
                if (signal <= 0)
                {
                    _lcdShield.Write(" ");
                }
                else if (signal == 8)
                {
                    _lcdShield.WriteByte(255);
                }
                else
                {
                    _lcdShield.WriteByte((byte)signal);
                }
            }
            _lcdShield.WriteLine(3, ("Sats: " + numberOfSatsUsedInFix + "/" + numberOfSatsTracked).PadRight(17, ' ') + GetAlarmIndicatorString());

        }

        static void DisplayPRNScreen()
        {

            var mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdShield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));

            for (var i = 0; i < _thunderbolt.Satellites.Length; i++)
            {
                var satellite = _thunderbolt.Satellites[i];
                //Debug.WriteLine($"Sat {i+1}" + satellite);
                if (satellite.Tracked)
                {
                    _prns[satellite.Channel] = i + 1;
                }
            }

            _lcdShield.SetCursorPosition(0, 1);
            for (var i = 0; i < _prns.Length / 2; i++)
            {

                if ((_thunderbolt.MinorAlarms & 0x800) == 0x800 && _prns[i] == 0)
                {
                    _lcdShield.Write("?? ");
                }
                else
                {
                    _lcdShield.Write(_prns[i].ToString().PadLeft(2, '0') + " ");
                }
            }
            _lcdShield.Write("".PadLeft(2));

            _lcdShield.SetCursorPosition(0, 2);
            for (var i = _prns.Length / 2; i < _prns.Length; i++)
            {
                if ((_thunderbolt.MinorAlarms & 0x800) == 0x800 && _prns[i] == 0)
                {
                    _lcdShield.Write("?? ");
                }
                else
                {
                    _lcdShield.Write(_prns[i].ToString().PadLeft(2, '0') + " ");
                }
            }
            _lcdShield.Write("".PadLeft(2));

            _lcdShield.WriteLine(3, "Satellite PRNs".PadRight(17, ' ') + GetAlarmIndicatorString());

        }

        static void DisplayDOPScreen()
        {
            _lcdShield.WriteLine(0, ("PDOP:" + _thunderbolt.PDOP.ToString("F3") + " " + Helpers.FloatToFixPrecisionString(_thunderbolt.PDOP)).PadRight(20, ' '));
            _lcdShield.WriteLine(1, ("TDOP:" + _thunderbolt.TDOP.ToString("F3") + " " + Helpers.FloatToFixPrecisionString(_thunderbolt.TDOP)).PadRight(20, ' '));
            _lcdShield.WriteLine(2, ("VDOP:" + _thunderbolt.VDOP.ToString("F3") + " " + Helpers.FloatToFixPrecisionString(_thunderbolt.VDOP)).PadRight(20, ' '));
            _lcdShield.WriteLine(3, ("HDOP:" + _thunderbolt.HDOP.ToString("F3") + " " + Helpers.FloatToFixPrecisionString(_thunderbolt.HDOP)).PadRight(17, ' ') + GetAlarmIndicatorString());

        }

#if(NTP)
        static void DisplayScreenNTP()
        {
            string mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdShield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));
            _lcdShield.WriteLine(1, "IP : " + NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress);
            _lcdShield.WriteLine(2, "SUB: " + NetworkInterface.GetAllNetworkInterfaces()[0].SubnetMask);
            _lcdShield.WriteLine(3, "MAC: " + GetMACAddress() + StringExtension.PadLeft(GetAlarmIndicatorString(), 3));
        }

        /// <summary>
        /// Gets the physical address of the primary network interface.  Returns a lower case string with no separators.
        /// </summary>
        /// <returns></returns>
        private static string GetMACAddress()
        {
            try
            {
                var netIf = NetworkInterface.GetAllNetworkInterfaces();

                string macAddress = "";

                // Create a character array for hexidecimal conversion.
                const string hexChars = "0123456789abcdef";

                // Loop through the bytes.
                for (int b = 0; b < 6; b++)
                {
                    // Grab the top 4 bits and append the hex equivalent to the return string.
                    macAddress += hexChars[netIf[0].PhysicalAddress[b] >> 4];

                    // Mask off the upper 4 bits to get the rest of it.
                    macAddress += hexChars[netIf[0].PhysicalAddress[b] & 0x0F];

                }

                return macAddress;
            }
            catch
            {
                // ...
                return "";
            }
        }

#endif
        static string GetAlarmIndicatorString()
        {
            var s = _thunderbolt.CriticalAlarms != 0 ? "C" : " ";
            s = s + (_thunderbolt.MinorAlarms != 0 ? "M" : " ");
            s = s + _pageNumber.ToString();
            return s;
        }

        static void UpdateAlarmIndicators()
        {
            MinorLed.Write((_thunderbolt.MinorAlarms != 0));
            MajorLed.Write((_thunderbolt.CriticalAlarms != 0));
        }

    }
}
