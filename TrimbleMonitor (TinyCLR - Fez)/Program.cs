using System;
using System.Diagnostics;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Pwm;
using GHIElectronics.TinyCLR.Pins;
using MicroLiquidCrystal;
using TrimbleMonitor.Thunderbolt;

namespace TrimbleMonitor.TinyCLR.Fez
{
    public class Program
    {
        // Percentage you would like the LCD set to.  Between 0 and 100.
        const byte LcdBrightness = 100;

        private static DfRobotLcdShield _lcdShield;
        private static ThunderBolt _thunderbolt;

        private static int _pageNumber = 1;
        private static int _previousPageNumber = 1;
        private static int _numberOfPages = 7;

        private static bool _isSurveyInProgress;
        private static int[] _prns = new int[12];

        private static readonly GpioPin MinorLed = GpioController.GetDefault().OpenPin(FEZ.GpioPin.D12);
        private static readonly GpioPin MajorLed = GpioController.GetDefault().OpenPin(FEZ.GpioPin.D11);

        public static void Main()
        {

            MinorLed.SetDriveMode(GpioPinDriveMode.Output);
            MajorLed.SetDriveMode(GpioPinDriveMode.Output);

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

            _thunderbolt = new ThunderBolt("COM1", AngleUnits.Degrees, AltitudeUnits.Meters, FEZ.GpioPin.D13);

            _lcdShield.OnButtonPressed += LcdShieldOnOnButtonPressed;

            DisplaySplash();

            var pwmController = PwmController.FromName(FEZ.PwmChannel.Controller3.Id);
            pwmController.SetDesiredFrequency(10000);

            var backlight = pwmController.OpenChannel(FEZ.PwmChannel.Controller3.D10);
            backlight.SetActiveDutyCyclePercentage(LcdBrightness / 100d);
            backlight.Start();

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
                _thunderbolt.RequestSatelliteSignalLevels();
            }
        }

        private static void TestLeds()
        {
            MinorLed.Write(GpioPinValue.High);
            MajorLed.Write(GpioPinValue.High);
            Thread.Sleep(100);
            MinorLed.Write(GpioPinValue.Low);
            MajorLed.Write(GpioPinValue.Low);
            Thread.Sleep(100);
            MinorLed.Write(GpioPinValue.High);
            MajorLed.Write(GpioPinValue.Low);
            Thread.Sleep(100);
            MinorLed.Write(GpioPinValue.Low);
            MajorLed.Write(GpioPinValue.Low);
            Thread.Sleep(100);
            MinorLed.Write(GpioPinValue.Low);
            MajorLed.Write(GpioPinValue.High);
            Thread.Sleep(100);
            MinorLed.Write(GpioPinValue.Low);
            MajorLed.Write(GpioPinValue.Low);
            Thread.Sleep(100);
            MinorLed.Write(GpioPinValue.High);
            MajorLed.Write(GpioPinValue.High);
            Thread.Sleep(100);
            MinorLed.Write(GpioPinValue.Low);
            MajorLed.Write(GpioPinValue.Low);
        }

        private static void LcdShieldOnOnButtonPressed(object sender, DfRobotLcdShield.Buttons buttonPressed)
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
            _lcdShield.WriteLine(3, "Ver: 2.0.0 (TinyCLR)", TextAlign.Centre);
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
                //var uptime = PowerState.Uptime;
                //_lcdshield.WriteLine(3, StringExtension.PadRight("Up:" + StringExtension.PadLeft(uptime.Days.ToString(), 4) + "D:" + StringExtension.PadLeft(uptime.Hours.ToString(), 2, '0') + "H:" + StringExtension.PadLeft(uptime.Minutes.ToString(), 2, '0') + "M", 17) + GetAlarmIndicatorString());
                _lcdShield.WriteLine(3, "".PadRight(17) + GetAlarmIndicatorString());
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

                // Only display information for satellites which have a channel number assigned.
                if (satellite.Tracked && satellite.CollectingData > 0)
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
                if (satellite.Tracked)
                {
                    _prns[satellite.Channel] = i + 1;
                }
            }

            for (var i = 0; i < _prns.Length; i++)
            {
                Debug.WriteLine(_prns[i].ToString());
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
        static string GetAlarmIndicatorString()
        {
            var s = _thunderbolt.CriticalAlarms != 0 ? "C" : " ";
            s += (_thunderbolt.MinorAlarms != 0 ? "M" : " ");
            s += _pageNumber.ToString();
            return s;
        }

        static void UpdateAlarmIndicators()
        {
            MinorLed.Write(_thunderbolt.MinorAlarms != 0 ? GpioPinValue.High : GpioPinValue.Low);
            MajorLed.Write(_thunderbolt.CriticalAlarms != 0 ? GpioPinValue.High : GpioPinValue.Low);
        }

    }
}
