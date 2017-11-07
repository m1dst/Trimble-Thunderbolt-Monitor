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

namespace TrimbleMonitor
{
    public class Program
    {

        static DfRobotLcdShield _lcdshield;
        static ThunderBolt _thunderbolt;
#if(NTP)
        static NtpServer _ntpServer;
#endif

        static int _pageNumber = 1;
        static int _previousPageNumber = 1;
#if(NTP)
        static int _numberOfPages = 5;
#else
        static int _numberOfPages = 4;
#endif

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
            _lcdshield = new DfRobotLcdShield(20, 4);

            // Create a custom degrees symbol (°), store it on the LCD for later use with the lat/long display
            _lcdshield.CreateChar(7, new byte[] { 0x0, 0x4, 0xa, 0x4, 0x0, 0x0, 0x0, 0x0 });

#if(NETDUINO)
            _thunderbolt = new ThunderBolt("COM1", AngleUnits.Degrees, AltitudeUnits.Meters, new OutputPort(Pins.GPIO_PIN_D13, false));
#endif
#if(FEZLEMUR)
            _thunderbolt = new ThunderBolt("COM1", AngleUnits.Degrees, AltitudeUnits.Meters, new OutputPort(GHI.Pins.FEZLemur.Gpio.D13, false));
#endif

#if(NTP)
            _ntpServer = new NtpServer();
#endif

            _lcdshield.OnButtonPressed += LcdshieldOnOnButtonPressed;

            DisplaySplash();
            TestLeds();

            _thunderbolt.Open();
            _thunderbolt.TimingMode = TimingModes.UTC;

            Thread.Sleep(1500);

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
#if(NTP)
                        case 5:
                            DisplayScreenFive();
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

                Thread.Sleep(100);

            }

        }

        private static void ThunderboltOnTimeChanged(object sender, EventArgs eventArgs)
        {
            if (_thunderbolt.IsSerialDataBeingReceived)
            {
                switch (_pageNumber)
                {
                    case 1:
                    case 2:
                    case 3:
#if(NTP)
                    case 5:
#endif
                        string mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
                        _lcdshield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));
                    break;
                }
            }
        }

        private static void TestLeds()
        {
            MinorLed.Write(true);
            MajorLed.Write(true);
            Thread.Sleep(500);
            MinorLed.Write(false);
            MajorLed.Write(false);
            Thread.Sleep(500);
            MinorLed.Write(true);
            MajorLed.Write(false);
            Thread.Sleep(500);
            MinorLed.Write(false);
            MajorLed.Write(false);
            Thread.Sleep(500);
            MinorLed.Write(false);
            MajorLed.Write(true);
            Thread.Sleep(500);
            MinorLed.Write(false);
            MajorLed.Write(false);
            Thread.Sleep(500);
            MinorLed.Write(true);
            MajorLed.Write(true);
            Thread.Sleep(500);
            MinorLed.Write(false);
            MajorLed.Write(false);
        }

        private static void LcdshieldOnOnButtonPressed(object sender, DfRobotLcdShield.Buttons buttonPressed)
        {
            switch (buttonPressed)
            {
                case DfRobotLcdShield.Buttons.Command1:
                    _thunderbolt.ReceiverMode = ReceiverMode.FullPosition;
                    // Transmit COM 8 Packet ID: BB  Data Length: 40
                    // 00 04 FF 04 02 3E 32 B8 C3 40 80 00 00 41 00 00 00 40 C0 00 00 FF 01 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF 

                    break;

                case DfRobotLcdShield.Buttons.Command2:

                    _thunderbolt.ReceiverMode = ReceiverMode.OverDeterminedClock;
                    // Transmit COM 8 Packet ID: BB  Data Length: 40
                    // 00 07 FF 04 02 3E 32 B8 C3 40 80 00 00 41 00 00 00 40 C0 00 00 FF 01 FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF 

                    break;

                case DfRobotLcdShield.Buttons.Command3:
                    //_thunderbolt.set_survey_params(1, 1, 5);
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
            _lcdshield.Clear();
            _lcdshield.WriteLine(0, "Trimble Thunderbolt", TextAlign.Centre);
            _lcdshield.WriteLine(1, "Monitor (M1DST)", TextAlign.Centre);
            _lcdshield.WriteLine(2, "www.m1dst.co.uk", TextAlign.Centre);
            _lcdshield.WriteLine(3, "Version 1.0.6", TextAlign.Centre);
        }

        static void DisplayNoSerialDataScreen()
        {
            _lcdshield.WriteLine(0, "No RS232 data avail.");
            _lcdshield.WriteLine(1, "Config GPS: 9600 8N1");
            _lcdshield.WriteLine(2, "Data: DB9 pin 2");
            _lcdshield.WriteLine(3, "GND:  DB9 pin 5");
        }

        static void DisplayScreenOne()
        {
            string mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdshield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));
            _lcdshield.WriteLine(1, "  GPS: " + EnumerationStrings.ReceiverStatusString(_thunderbolt.ReceiverStatus));
            _lcdshield.WriteLine(2, "DActv: " + EnumerationStrings.DiscipliningActivityString(_thunderbolt.DisciplineActivity));
            _lcdshield.WriteLine(3, "10MHz: " + StringExtension.PadLeft(_thunderbolt.Osc_Offset.ToString("N3") + "ppb ", 10) + GetAlarmIndicatorString());
        }

        static void DisplayScreenTwo()
        {
            string mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdshield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));
            _lcdshield.WriteLine(1, " RX M: " + EnumerationStrings.ReceiverModeString(_thunderbolt.ReceiverMode));
            _lcdshield.WriteLine(2, "DscpM: " + EnumerationStrings.DiscipliningModeString(_thunderbolt.DisciplineMode));
            if (_isSurveyInProgress)
            {
                _lcdshield.WriteLine(3, "Survey: " + StringExtension.PadRight(StringExtension.PadLeft(_thunderbolt.SurveyProgress + "%", 4), 9) + GetAlarmIndicatorString());
            }
            else
            {
                var uptime = PowerState.Uptime;
                _lcdshield.WriteLine(3, StringExtension.PadRight("Up:" + StringExtension.PadLeft(uptime.Days.ToString(), 4) + "D:" + StringExtension.PadLeft(uptime.Hours.ToString(), 2, '0') + "H:" + StringExtension.PadLeft(uptime.Minutes.ToString(), 2, '0') + "M", 17) + GetAlarmIndicatorString());
            }

        }

        static void DisplayScreenThree()
        {
            string mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdshield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));

            _lcdshield.SetCursorPosition(0, 1);
            var s = "Lat:" + StringExtension.PadLeft(_thunderbolt.CurrentPosition.Latitude.ToString("N4"), 9);
            _lcdshield.Write(s);
            _lcdshield.WriteByte(7);
            _lcdshield.Write(StringExtension.PadLeft("", 16 - (s.Length + 1)));
            _lcdshield.Write("Alt:");

            _lcdshield.SetCursorPosition(0, 2);
            s = "Lon:" + StringExtension.PadLeft(_thunderbolt.CurrentPosition.Longitude.ToString("N4"), 9);
            _lcdshield.Write(s);
            _lcdshield.WriteByte(7);
            _lcdshield.Write(StringExtension.PadLeft("", 14 - (s.Length + 1)));
            _lcdshield.Write(StringExtension.PadLeft(_thunderbolt.CurrentPosition.Altitude.ToString("N0") + "m", 6));

            _lcdshield.WriteLine(3, "Grid: " + StringExtension.PadRight(MaidenheadLocator.LatLongToLocator(_thunderbolt.CurrentPosition.Latitude, _thunderbolt.CurrentPosition.Longitude), 11) + GetAlarmIndicatorString());

        }

        static void DisplayScreenFour()
        {
            if (_thunderbolt.CriticalAlarms == 0)
            {
                _lcdshield.WriteLine(0, "No critical alarms");
            }
            else
            {
                if ((_thunderbolt.CriticalAlarms & 0x01) == 0x01)
                {
                    _lcdshield.WriteLine(0, "ROM checksum");
                }
                else if ((_thunderbolt.CriticalAlarms & 0x02) == 0x02)
                {
                    _lcdshield.WriteLine(0, "RAM check");
                }
                else if ((_thunderbolt.CriticalAlarms & 0x04) == 0x04)
                {
                    _lcdshield.WriteLine(0, "PSU fault");
                }
                else if ((_thunderbolt.CriticalAlarms & 0x08) == 0x08)
                {
                    _lcdshield.WriteLine(0, "FPGA check");
                }
                else if ((_thunderbolt.CriticalAlarms & 0x10) == 0x10)
                {
                    _lcdshield.WriteLine(0, "Osc. control voltage");
                }

            }

            if (_thunderbolt.MinorAlarms == 0)
            {
                _lcdshield.WriteLine(1, "No minor alarms");
            }
            else
            {

                if ((_thunderbolt.MinorAlarms & 0x01) == 0x01)
                {
                    _lcdshield.WriteLine(1, "Osc. control voltage");
                }
                else if ((_thunderbolt.MinorAlarms & 0x02) == 0x02)
                {
                    _lcdshield.WriteLine(1, "No antenna connected");
                }
                else if ((_thunderbolt.MinorAlarms & 0x04) == 0x04)
                {
                    _lcdshield.WriteLine(1, "Antenna shorted");
                }
                else if ((_thunderbolt.MinorAlarms & 0x08) == 0x08)
                {
                    _lcdshield.WriteLine(1, "No usable satellites");
                }
                else if ((_thunderbolt.MinorAlarms & 0x10) == 0x10)
                {
                    _lcdshield.WriteLine(1, "Not disciplining");
                }
                else if ((_thunderbolt.MinorAlarms & 0x20) == 0x20)
                {
                    _lcdshield.WriteLine(1, "Survey in progress");
                }
                else if ((_thunderbolt.MinorAlarms & 0x40) == 0x40)
                {
                    _lcdshield.WriteLine(1, "No stored position");
                }
                else if ((_thunderbolt.MinorAlarms & 0x80) == 0x80)
                {
                    _lcdshield.WriteLine(1, "Leap second pending");
                }
                else if ((_thunderbolt.MinorAlarms & 0x100) == 0x100)
                {
                    _lcdshield.WriteLine(1, "In test mode");
                }
                else if ((_thunderbolt.MinorAlarms & 0x200) == 0x200)
                {
                    _lcdshield.WriteLine(1, "Pos. questionable");
                }
                else if ((_thunderbolt.MinorAlarms & 0x400) == 0x400)
                {
                    _lcdshield.WriteLine(1, "EEPROM invalid");
                }
                else if ((_thunderbolt.MinorAlarms & 0x800) == 0x800)
                {
                    _lcdshield.WriteLine(1, "Almanac not complete");
                }
            }

            _lcdshield.WriteLine(2, "DAC: " + _thunderbolt.DAC_Voltage.ToString("N6") + "V");

            _lcdshield.SetCursorPosition(0, 3);
            _lcdshield.Write("Temp: " + _thunderbolt.Temperature.ToString("N2"));
            _lcdshield.WriteByte(7);
            _lcdshield.Write("C    " + GetAlarmIndicatorString());
        }

#if(NTP)
        static void DisplayScreenFive()
        {
            string mode = _thunderbolt.TimingMode == TimingModes.UTC ? "U" : "G";
            _lcdshield.WriteLine(0, DateTime.UtcNow.ToString(@"dd-MMM-yy \" + mode + " HH:mm:ss"));
            _lcdshield.WriteLine(1, "IP : " + NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress);
            _lcdshield.WriteLine(2, "SUB: " + NetworkInterface.GetAllNetworkInterfaces()[0].SubnetMask);
            _lcdshield.WriteLine(3, "MAC: " + GetMACAddress() + StringExtension.PadLeft(GetAlarmIndicatorString(), 3));
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
