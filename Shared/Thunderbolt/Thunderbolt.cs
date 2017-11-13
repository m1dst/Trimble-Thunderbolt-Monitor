using System;
using Microsoft.SPOT;
using System.IO.Ports;
using System.Threading;
using System.Collections;
using Microsoft.SPOT.Hardware;

namespace TrimbleMonitor.Thunderbolt
{
    class ThunderBolt
    {

        #region Class data

        FixedSizedQueue PacketQueue;
        Thread PacketProcessing;

        private SerialPort m_port;
        public static OutputPort _activityLed;

        private string _levelType;
        /// <summary>
        /// tbd
        /// </summary>
        public string LevelType
        {
            get { return _levelType; }
        }

        private VersionInfo _firmwareVersion = new VersionInfo();
        private VersionInfo _hardwareVersion = new VersionInfo();
        private VersionInfo _buildVersion = new VersionInfo();

        private bool _pps;
        private bool _oscPolarity;
        private byte _ppsMode;
        private byte unit_survey_save;
        private UInt32 unit_survey_length;

        private ReceiverMode _receiverMode = ReceiverMode.Unknown;
        /// <summary>
        /// Returns or sets the current mode of the receiver
        /// </summary>
        public ReceiverMode ReceiverMode
        {
            get { return _receiverMode; }
            set
            {
                _receiverMode = value;
                set_rcvr_config();
            }
        }

        private DiscipliningMode _disciplineMode = DiscipliningMode.Unknown;
        /// <summary>
        /// Returns the current disciplining mode of the receiver
        /// </summary>
        public DiscipliningMode DisciplineMode
        {
            get { return _disciplineMode; }
            set { _disciplineMode = value; }
        }

        private byte _surveyProgress;
        /// <summary>
        /// Returns current survey progress (0-100%)
        /// </summary>
        public byte SurveyProgress
        {
            get { return _surveyProgress; }
        }

        public bool IsSurveyInProgress()
        {
            return ((MinorAlarms & 0x20) == 0x20);
        }

        private UInt32 _holdoverDuration;

        /// <summary>
        /// Returns current Holdover Duration (secs)
        /// </summary>
        public UInt32 HoldoverDuration
        {
            get { return _holdoverDuration; }
        }

        private UInt16 _criticalAlarms;
        /// <summary>
        /// Returns current Critical alarm bit field.
        /// </summary>
        public UInt16 CriticalAlarms
        {
            get { return _criticalAlarms; }
        }

        private UInt16 _minorAlarms;
        /// <summary>
        /// Returns current Minor alarm bit field
        /// </summary>
        public UInt16 MinorAlarms
        {
            get { return _minorAlarms; }
        }

        private ReceiverStatus _gpsReceiverStatus = ReceiverStatus.Unknown;
        /// <summary>
        /// Returns current GPS receiver status
        /// </summary>
        public ReceiverStatus GpsReceiverReceiverStatus
        {
            get { return _gpsReceiverStatus; }
        }

        private DiscipliningActivity _disciplineActivity;
        /// <summary>
        /// Returns current disciplining activity
        /// </summary>
        public DiscipliningActivity DisciplineActivity
        {
            get { return _disciplineActivity; }
        }

        private Single _ppsOffset;
        /// <summary>
        /// Returns an estimate of the offset of the PPS output relative to UTC or GPS as
        /// reported by the GPS receiver in nanoseconds. A positive values indicate that 
        /// the receiver's PPS is coming out late relative to GPS or UTC.
        /// </summary>
        public Single PpsOffset
        {
            get { return _ppsOffset; }
        }

        private Single _oscOffset;
        /// <summary>
        /// Returns an estimate of the frequency offset of the 10MHz output relative to
        /// UTC or GPS as reported by the GPS receiver in ppb (parts-per-billion.) Positive values
        /// indicate that the receiver's clock is running slow relative to GPS or UTC.
        /// </summary>
        public Single OscOffset
        {
            get { return _oscOffset; }
        }

        private UInt32 _dacValue;
        /// <summary>
        /// Returns current numeric value of the DAC used to produce the voltage that
        /// controls the frequency of the 10MHz oscillator.
        /// </summary>
        public UInt32 DacValue
        {
            get { return _dacValue; }
        }

        private Single _dacVoltage;
        /// <summary>
        /// Returns current voltage output of the DAC
        /// </summary>
        public Single DacVoltage
        {
            get { return _dacVoltage; }
        }

        private Single _temperature;
        /// <summary>
        /// Returns the current temperature (in Celsius) as reported by the receiver's 
        /// on-board temperature sensor
        /// </summary>
        public Single Temperature
        {
            get { return _temperature; }
        }

        private Position _currentPosition;
        private Position last_position;

        public SatInfo[] Satellites = new SatInfo[32];

        private DateTime _currentTime;
        private DateTime last_current_time;

        private UInt32 _timeOfWeek;
        private TimeType _timeValue;
        private UInt16 _gpsWeek;
        private Int16 _utcOffset;

        private Single _dampingFactor = 0;
        private Single _oscGain = 0;
        private Single _minVolts = 0;
        private Single _maxVolts = 0;
        private Single _jamSync = 0;
        private Single _maximumFrequencyOffset = 0;
        private Single _initialVoltage = 0;
        private Single _timeConstant = 0;

        private Double _cableDelay;
        private byte pv_filter;
        private byte static_filter;
        private byte altitude_filter;
        private byte kalman_filter;

        private FixPrecision positional_dop;
        private FixPrecision temporal_dop;
        private FixPrecision horizontal_dop;
        private FixPrecision vertical_dop;
        private FixDimension fix_dimension;
        private FixMode fix_mode;

        private Int64 loopsSinceDataReceived = 0;

        private ReceiverStatus receiver_status;

        #endregion

        #region Event declarations

        public event ThunderBoltEventHandler TimeChanged;
        public event ThunderBoltEventHandler PositionChanged;
        public event ThunderBoltEventHandler SecondaryTimingChanged;
        public event VersionInfoEventHandler SoftwareVersionInfoReceived;
        public event VersionInfoEventHandler FirmwareVersionInfoReceived;
        public event VersionInfoEventHandler HardwareVersionInfoReceived;
        public event GpsTimeReceivedEventHandler GpsTimeReceived;
        //public event ThunderBoltEventHandler ThunderboltNotReceivingDataError;
        //public event ThunderBoltEventHandler ThunderboltNotReceivingDataErrorCleared;

        #endregion

        bool worker_running = true;

        public static void Reverse(byte[] array)
        {
            int length = array.Length;
            int mid = (length / 2);

            for (int i = 0; i < mid; i++)
            {
                byte b = array[i];
                array[i] = array[length - i - 1];
                array[length - i - 1] = b;
            }
        }

        public ThunderBolt(string portName, AngleUnits au, AltitudeUnits tu, OutputPort activityLed)
        {
            _activityLed = activityLed;
            PacketQueue = new FixedSizedQueue(10);

            PacketProcessing = new Thread(new ThreadStart(worker_thread));

            _currentPosition = new Position(au, tu);
            last_position = new Position(au, tu);
            m_port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            m_port.DataReceived += new SerialDataReceivedEventHandler(m_port_DataReceived);
            m_port.ErrorReceived += new SerialErrorReceivedEventHandler(m_port_ErrorReceived);
            for (int i = 0; i < 32; i++)
                Satellites[i] = new SatInfo();
        }

        #region Packet Parsing and Dispatch

        TsipPacket current_packet = new TsipPacket();  // Holds the current packet being serviced by the serial port DataReceived event

        private void m_port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _activityLed.Write(true);  // turn on the LED 
            while (m_port.BytesToRead > 0)
            {

                current_packet.AddByte((ushort)m_port.ReadByte());
                if (current_packet.IsComplete)                  // received complete packet?
                {
                    loopsSinceDataReceived = 0; // reset the no data being received loop counter

                    lock (((ICollection)PacketQueue).SyncRoot)  // use lock for thread sync
                    {
                        PacketQueue.Enqueue(current_packet);    // queue up the packet for the worker thread
                    }
                    current_packet = new TsipPacket();          // allocate a new packet
                }
            }
            _activityLed.Write(false);  // turn off the LED 
        }

        private void m_port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {

        }

        public bool IsSerialDataBeingReceived
        {
            get { return (loopsSinceDataReceived < 400); }

        }

        /// <summary>
        /// Worker thread for processing received TSIP packets
        /// </summary>
        private void worker_thread()
        {
            TsipPacket tp = null;
            while (worker_running)
            {
                loopsSinceDataReceived++;
                //Debug.Print("There have been " + loopsSinceDataReceived + " loops since data received.");
                Thread.Sleep(10);
                lock (((ICollection)PacketQueue).SyncRoot)  // use a lock for thread sync
                {
                    //Debug.Print("There are " + PacketQueue.Count + " packets in the queue");

                    if (PacketQueue.Count > 0)
                        tp = (TsipPacket)PacketQueue.Dequeue();         // dequeue a packet if one is available
                }
                if (tp != null)
                {
                    process_packet(tp);                     // process then discard the packet
                    tp = null;
                }
            }
        }

        private int unparseable_packet_count = 0;

        /// <summary>
        /// Dispatches a packet to the appropriate decoding routine
        /// </summary>
        /// <param name="tp"></param>
        private void process_packet(TsipPacket tp)
        {
            //Debug.Print(tp.ToString());

            switch (tp.PacketType)
            {
                case 0x13: // unparsable packet
                    {
                        unparseable_packet_count++;
                        Debug.Print("Unparseable Packet: " + tp.PacketType.ToString("X"));
                        break;
                    }
                case 0x1C: // Thunderbolt E version info
                    {
                        ebolt_version(tp);
                        break;
                    }
                case 0x41: // GPS time received
                    {
                        gps_time_received(tp);
                        break;
                    }
                case 0x42: // 
                    {
                        single_ecef_fix(tp);
                        break;
                    }
                case 0x43: //
                    {
                        velocity_fix(tp);
                        break;
                    }
                case 0x45: // version_info();
                    {
                        version_info(tp);
                        break;
                    }
                case 0x46: // 
                    {
                        ebolt_health1(tp);
                        break;
                    }
                case 0x47: // 
                    {
                        receive_sig_levels(tp);
                        break;
                    }
                case 0x49: // 
                    {
                        get_alm_health(tp);
                        break;
                    }
                case 0x4B: // 
                    {
                        ebolt_health2(tp);
                        break;
                    }
                case 0x55: // 
                    {
                        io_options(tp);
                        break;
                    }
                case 0x56: // 
                    {
                        enu_velocity_fix(tp);
                        break;
                    }
                case 0x57: // 
                    {
                        last_fix_info(tp);
                        break;
                    }
                case 0x58: // 
                    {
                        packet_58(tp);
                        break;
                    }
                case 0x59: // 
                    {
                        sat_health(tp);
                        break;
                    }
                case 0x5A: // 
                    {
                        raw_data(tp);
                        break;
                    }
                case 0x5B: // 
                    {
                        eph_status(tp);
                        break;
                    }
                case 0x5C: // 
                    {
                        sat_tracking(tp);
                        break;
                    }
                case 0x5F: // 
                    {
                        eeprom_status(tp);
                        break;
                    }
                case 0x6D: // 
                    {
                        sat_list(tp);
                        break;
                    }
                case 0x70: // 
                    {
                        filter_config(tp);
                        break;
                    }
                case 0x83: // 
                    {
                        ecef_fix(tp);
                        break;
                    }
                case 0x84: // 
                    {
                        lla_fix(tp);
                        break;
                    }
                case 0x8F: // 
                    {
                        timing_msg(tp);
                        break;
                    }
                case 0xBB: // 
                    {
                        rcvr_config(tp);
                        break;
                    }
                default:
                    unknown_msg(tp);
                    break;
            }

        }

        #endregion

        #region Initialization

        public void SetupUnitForDisciplining()
        {
            init_messages();
        }

        private void init_messages()
        {
            set_packet_mask(0x0055, 0x0000);  // floating
            // set_packet_mask(0x0065, 0x0000);  // integer
            set_io_options(0x13, 0x03, 0x01, 0x09);  // ECEF+LLA+DBL PRECISION, ECEF+ENU vel,  UTC, PACKET 5A
            //set_io_options(0x13, 0x03, 0x01, 0x08);  // ECEF+LLA+DBL PRECISION, ECEF+ENU vel,  UTC, no PACKET 5A

            request_primary_timing(); // get time of day

            request_rcvr_info();      // get various receiver status messages
            request_rcvr_health();

            request_sig_levels();     // satellite info
            request_last_raw(0x00);
            request_sat_status(0x00);
            request_eph_status(0x00);

            set_timing_mode();

            //if (user_set_delay || set_pps_polarity)
            //{   // set cable delay for 50 feet of 0.66 vel factor cable
            //    set_pps(user_pps_enable, user_pps_polarity, delay_value, 300.0);
            //}

            //if (set_osc_polarity)
            //{
            //    set_osc_sense(user_osc_polarity);
            //}

            //if (do_survey)
            //{
            //    set_survey_params(1, 1, do_survey);
            //    start_self_survey();
            //}
        }

        private void request_rcvr_info()
        {
            request_software_version();
            request_manuf_params();
            request_prodn_params();
            request_fw_ver();       // ThunderBolt-E only
            request_hw_ver();       // ThunderBolt-E only
            request_pps();
            request_timing_mode();
            request_filter_config();
            request_survey_params();
            request_all_dis_params();
        }

        #endregion

        #region Test routines

        //private int req_num = 0;  //TEST

        //private void request_misc_msg()
        //{
        //    // This routine requests various minor status messages
        //    // It requests a different message each time it is called.
        //    ++req_num;

        //    if (req_num == 1) request_io_options();
        //    else if (req_num == 2) request_alm_health();
        //    else if (req_num == 3) request_manuf_params();
        //    else if (req_num == 4) request_last_posn();
        //    else if (req_num == 5) request_software_version();
        //    else if (req_num == 6) request_sig_levels();
        //    else if (req_num == 7) request_eph_status(0x00);
        //    else if (req_num == 8) request_sat_status(0x00);
        //    else if (req_num == 9) request_eeprom_status();
        //    else if (req_num == 10) request_sat_list();
        //    else if (req_num == 11) request_filter_config();
        //    else if (req_num == 12) request_prodn_params();
        //    else if (req_num == 13) request_pps();
        //    else if (req_num == 14) request_dac_voltage();
        //    else if (req_num == 15) request_osc_sense();
        //    else if (req_num == 16) request_timing_mode();
        //    else if (req_num == 17) request_packet_mask();
        //    else if (req_num == 18) request_survey_params();
        //    else if (req_num == 19) request_last_raw(0x00);
        //    else if (req_num == 20) request_all_dis_params();
        //    else if (req_num == 21) request_sat_health();
        //    else if (req_num == 22) request_rcvr_config();
        //    else if (req_num == 23) request_datum();
        //    else if (req_num == 24) Debug.Print("Last Request");
        //    else req_num = 0;
        //}

        #endregion

        #region Properties

        /// <summary>
        /// Returns the current UTC or GPS time
        /// </summary>
        public DateTime CurrentTime
        {
            get { return _currentTime; }
        }

        public Single TimeConstant
        {
            get
            {
                return _timeConstant;
            }
            set
            {
                _timeConstant = value;
            }
        }

        public Single InitialVoltage
        {
            get
            {
                return _initialVoltage;
            }
            set
            {
                _initialVoltage = value;
            }
        }

        public Single MaximumFrequencyOffset
        {
            get
            {
                return _maximumFrequencyOffset;
            }
            set
            {
                _maximumFrequencyOffset = value;
            }
        }

        public Single JamSync
        {
            get
            {
                return _jamSync;
            }
            set
            {
                _jamSync = value;
            }
        }

        public Single MaxVolts
        {
            get
            {
                return _maxVolts;
            }
            set
            {
                _maxVolts = value;
            }
        }

        public Single MinVolts
        {
            get
            {
                return _minVolts;
            }
            set
            {
                _minVolts = value;
            }
        }

        public Single OscGain
        {
            get
            {
                return _oscGain;
            }
            set
            {
                _oscGain = value;
            }
        }

        public Single DampingFactor
        {
            get
            {
                return _dampingFactor;
            }
            set
            {
                _dampingFactor = value;
            }
        }

        public TimeType TimeValue
        {
            get { return _timeValue; }
        }

        public UInt32 TimeOfWeek
        {
            get { return _timeOfWeek; }
        }

        public UInt16 GpsWeek
        {
            get { return _gpsWeek; }
        }

        public Int16 UtcOffset
        {
            get { return _utcOffset; }
        }

        public bool Pps
        {
            get { return _pps; }

            set
            {
                _pps = value;
            }
        }

        public Double CableDelay
        {
            get
            {
                return _cableDelay;
            }
        }

        public bool OscPolarity
        {
            get { return _oscPolarity; }
        }

        public byte PpsMode
        {
            get { return _ppsMode; }
        }

        public Position CurrentPosition
        {
            get { return _currentPosition; }
        }

        internal VersionInfo FirmwareVersion
        {
            get { return _firmwareVersion; }
        }

        internal VersionInfo HardwareVersion
        {
            get { return _hardwareVersion; }
        }

        internal VersionInfo BuildVersion
        {
            get { return _buildVersion; }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Opens the serial port attached to the Thunderbolt hardware. Returns true if the port opened, otherwise false.
        /// </summary>
        public bool Open()
        {
            try
            {
                m_port.Open();
                if (m_port.IsOpen)
                {
                    PacketProcessing.Start();
                }
                return m_port.IsOpen;
            }
            catch
            {
                return false;
            }
        }

        public void Close()
        {
            worker_running = false;

            if (m_port.IsOpen)
                m_port.Close();
        }

        #endregion

        private void unknown_msg(TsipPacket tp)
        {
            Debug.Print("Unknown TSIP msg: " + tp.ToString());
            unparseable_packet_count++;
        }

        private void datums(TsipPacket tp)
        {
            Debug.Print("Packet 8F.15 (Datums)");
            int index = tp.GetNextWord();
            double dx = tp.GetNextDouble();
            double dy = tp.GetNextDouble();
            double dz = tp.GetNextDouble();
            double a_axis = tp.GetNextDouble();
            double ecc = tp.GetNextDouble();
        }

        #region TSIP packet decoding

        private void manuf_params(TsipPacket tp)
        {
            Debug.Print("Packet 8F.41 (Manufacturing Params):");

            UInt16 sn_prefix = tp.GetNextWord();
            UInt32 serial_num = tp.GetNextDWord();
            byte build_year = tp.GetNextByte();
            byte build_month = tp.GetNextByte();
            byte build_day = tp.GetNextByte();
            byte build_hour = tp.GetNextByte();
            Single osc_offset = tp.GetNextSingle();
            UInt16 test_code = tp.GetNextWord();

            _buildVersion.Date = new DateTime(build_year + 2000, build_month, build_day, build_hour, 0, 0);
            _buildVersion.SerialNumber = sn_prefix.ToString() + "-" + serial_num.ToString();
        }

        private void prodn_params(TsipPacket tp)
        {
            Debug.Print("Packet 8F.42 (Production Params):");

            byte prodn_options = tp.GetNextByte();
            byte prodn_number = tp.GetNextByte();
            UInt16 case_prefix = tp.GetNextWord();
            UInt32 case_sn = tp.GetNextDWord();
            UInt32 prodn_num = tp.GetNextDWord();
            UInt16 rsvd1 = tp.GetNextWord();
            UInt16 machine_id = tp.GetNextWord();
            UInt16 rsvd2 = tp.GetNextWord();
        }

        private void pps_settings(TsipPacket tp)
        {
            Debug.Print("Packet 8F.4A (PPS settings):");

            byte pps_enabled = tp.GetNextByte();
            byte pps_rsvd = tp.GetNextByte();
            byte pps_polarity = tp.GetNextByte();
            Double cable_delay = tp.GetNextDouble();
            Single bias_threshold = tp.GetNextSingle();

            _pps = (pps_enabled != 0);
            _cableDelay = (cable_delay / 1.0E-9);
        }

        private void dac_values(TsipPacket tp)
        {
            Debug.Print("Packet 8F.A0 (DAC values):");

            UInt32 dac_value = tp.GetNextDWord();
            Single dac_voltage = tp.GetNextSingle();
            Byte dac_res = tp.GetNextByte();
            Byte dac_format = tp.GetNextByte();
            Single dac_min = tp.GetNextSingle();
            Single dac_max = tp.GetNextSingle();
        }

        private void osc_sense(TsipPacket tp) // not avilable on ThunderBolt-E or early ThunderBolts
        {
            Debug.Print("Packet 8F.A1 (10 MHz sense):");
            _oscPolarity = (tp.GetNextByte() != 0);
        }

        private void pps_timing_mode(TsipPacket tp)
        {
            Debug.Print("Packet 8F.A2 (Timing mode):");
            _ppsMode = tp.GetNextByte();
        }

        private void packet_mask(TsipPacket tp)
        {
            Debug.Print("Packet 8F.A5 (Packet mask):");
            UInt16 mask1 = tp.GetNextWord();
            UInt16 mask2 = tp.GetNextWord();
        }

        /// <summary>
        /// This broadcast packet provides individual satellite solutions as well as the combined
        /// timing solutions. Two formats of this packet are supported: a floating-point form and an
        /// integer form. This packet is broadcast once per second if enabled with the 0x8E-A5 packet
        /// mask command. Packet 0x8E-A5 allows the user to select which format will be broadcast.
        /// 
        /// notes:
        /// If both formats are selected the ThunderBolt will send format 0 (floating point) only.
        /// 
        /// For clock bias numbers, a positive sign indicates that the ThunderBolt PPS occurs after the
        /// GPS PPS. 
        /// 
        /// For clock bias rate numbers, a positive sign indicates that the ThunderBolt
        /// 10MHz frequency output is running slow relative to GPS.
        /// 
        /// 8 sets of satellite ID and data are sent with n=1, 2, . . . 8
        /// </summary>
        private void sat_solutions(TsipPacket tp) //not available on ThunderBolt-E
        {
            Debug.Print(":0x8F.A7 (Satellite Solutions)");

            byte format = tp.GetNextByte();
            UInt32 time_of_week = tp.GetNextDWord();

            Single clock_bias;
            Single clock_bias_rate;

            if (format == 0)
            {   // floating point
                clock_bias = tp.GetNextSingle();
                clock_bias_rate = tp.GetNextSingle();
            }
            else if (format == 1)
            {   // integer values
                clock_bias = (float)(int)tp.GetNextWord();
                clock_bias *= 100.0e-12F;
                clock_bias_rate = (float)(int)tp.GetNextWord();
                clock_bias_rate *= 1.0e-12F;
            }
            else
            {
                unknown_msg(tp);
                return;
            }

            for (int i = 0; i < 32; i++)    // reset current bias flags
                Satellites[i].HasBiasInfo = false;

            for (int i = 0; i < 8; i++)     // get bias info from all visible satellites
            {
                byte prn = tp.GetNextByte();
                prn--;
                if (prn > 31)               // ignore bogus data
                    continue;

                if (format == 0)
                    Satellites[prn].SatBias = tp.GetNextSingle();
                else
                {
                    Satellites[prn].SatBias = (float)(int)tp.GetNextWord();
                    Satellites[prn].SatBias *= 100.0e-12F;
                }

                Satellites[prn].TimeOfFix = (float)time_of_week;
                Satellites[prn].HasBiasInfo = true;
            }
        }

        private void discipline_params(TsipPacket tp)
        {
            Debug.Print("Packet 8F.A8 (Discipline params):");

            byte type = tp.GetNextByte();

            if (type == 0)
            {
                _timeConstant = tp.GetNextSingle();
                _dampingFactor = tp.GetNextSingle();
            }
            else if (type == 1)
            {
                _oscGain = tp.GetNextSingle();
                _minVolts = tp.GetNextSingle();
                _maxVolts = tp.GetNextSingle();
            }
            else if (type == 2)
            {
                _jamSync = tp.GetNextSingle();
                _maximumFrequencyOffset = tp.GetNextSingle();
            }
            else if (type == 3)
            {
                _initialVoltage = tp.GetNextSingle();
            }
        }

        private void survey_params(TsipPacket tp)
        {
            Debug.Print("Packet 8F.A9 (Survey params):");
            byte survey_flag = tp.GetNextByte();
            unit_survey_save = tp.GetNextByte();
            unit_survey_length = tp.GetNextDWord();
            UInt32 rsvd = tp.GetNextDWord();
        }

        private void primary_timing(TsipPacket tp)
        {
            Debug.Print(":0x8F.AB (Primary Timing)");

            UInt32 pri_tow = tp.GetNextDWord();
            ushort pri_gps_week = tp.GetNextWord();
            pri_gps_week += 1024; // We needed to correct the GPS Week.  See below.

            // This field represents the current GPS week number. GPS week number 0 started on January 6, 1980.
            // Unfortunately, the GPS system has allotted only 10-bits of information to carry the GPS week number and 
            // therefore it rolls-over to 0 in just 1024 weeks (19.6 years,) and there is no mechanism built into GPS to 
            // tell the user to which 1024 week epoch the week number refers. The first week number roll-over occured
            // as August 21, 1999 (GPS) transitioned to August 22, 1999 (GPS).
            // The ThunderBolt adjusted for this week rollover by adding 1024 to any week number reported by GPS which is 
            // less that week number 936 which began on December 14, 1997. With this technique, the ThunderBolt  
            // provided an accurate translation of GPS week number and TOW to time and date until July 30, 2017.

            // Now it has passed July 30, 2017 we need to provide the same fix but in our code.  The Thunderbolt can no longer be trusted!

            short pri_utc_offset = (short)tp.GetNextWord();
            byte time_flags = tp.GetNextByte();
            byte pri_seconds = tp.GetNextByte();
            byte pri_minutes = tp.GetNextByte();
            byte pri_hours = tp.GetNextByte();
            byte pri_day = tp.GetNextByte();
            byte pri_month = tp.GetNextByte();
            ushort pri_year = tp.GetNextWord();

            try
            {
                
                // if this fires an execption, just skip the bad packet
                //current_time = new DateTime(pri_year, pri_month, pri_day, pri_hours, pri_minutes, pri_seconds);  // This is no longer reliable.
                _currentTime = new DateTime(1980, 1, 6).AddDays(pri_gps_week * 7).AddSeconds(pri_tow - pri_utc_offset); // We have to calculate the time ourselves.

                _timeOfWeek = pri_tow;
                _utcOffset = pri_utc_offset;
                _gpsWeek = pri_gps_week;

                if ((time_flags & 0x04) == 0x04)
                    _timeValue = TimeType.NoTimeAvailable;
                else if ((time_flags & 0x08) == 0x08)
                    _timeValue = TimeType.NoUTCOffset;
                else if ((time_flags & 0x10) == 0x10)
                    _timeValue = TimeType.UserSetTime;
                else if ((time_flags & 0x01) == 0x01)
                    _timeValue = TimeType.UTCTimeOk;
                else
                    _timeValue = TimeType.GPSTimeOk;

                // Set the time on the netduino to the current time on the Thunderbolt.
                Microsoft.SPOT.Hardware.Utility.SetLocalTime(_currentTime);

                if ((last_current_time != _currentTime) && (TimeChanged != null))
                    TimeChanged(this, new EventArgs());

                last_current_time = _currentTime;
            }
            catch (Exception e)
            {
                _currentTime = last_current_time;
                Debug.Print("Exception:" + e.Message);
            }
        }

        private void secondary_timing(TsipPacket tp)
        {
            Debug.Print(":0x8F.AC (Secondary Timing)");

            byte spare;
            try
            {
                _receiverMode = (ReceiverMode)tp.GetNextByte();
            }
            catch
            {
                _receiverMode = ReceiverMode.Unknown;
            }
            try
            {
                _disciplineMode = (DiscipliningMode)tp.GetNextByte();
            }
            catch
            {
                _disciplineMode = DiscipliningMode.Unknown;
            }
            _surveyProgress = tp.GetNextByte(); // 0-100%

            _holdoverDuration = tp.GetNextDWord(); // seconds

            _criticalAlarms = tp.GetNextWord();
            _minorAlarms = tp.GetNextWord();
            try
            {
                _gpsReceiverStatus = (ReceiverStatus)tp.GetNextByte();
            }
            catch
            {
                _gpsReceiverStatus = ReceiverStatus.Unknown;
            }

            try
            {
                _disciplineActivity = (DiscipliningActivity)tp.GetNextByte();
            }
            catch
            {
                _disciplineActivity = DiscipliningActivity.Unknown;
            }

            spare = tp.GetNextByte();
            spare = tp.GetNextByte();

            _ppsOffset = tp.GetNextSingle();  // in nano seconds (ns)

            _oscOffset = tp.GetNextSingle();  // in parts per billion (ppb)

            _dacValue = tp.GetNextDWord();
            _dacVoltage = tp.GetNextSingle(); // in V
            _temperature = tp.GetNextSingle(); // in C

            _currentPosition.Latitude = tp.GetNextDouble();
            _currentPosition.Longitude = tp.GetNextDouble();
            _currentPosition.Altitude = tp.GetNextDouble();

            if (SecondaryTimingChanged != null)
                SecondaryTimingChanged(this, null);

            raise_position_change();

            clear_sat_tracking(); //??
        }

        private void timing_msg(TsipPacket tp)
        {
            byte subcode = tp.GetNextByte();

            if (subcode == 0x15)
                datums(tp);
            else if (subcode == 0x41)
                manuf_params(tp);
            else if (subcode == 0x42)
                prodn_params(tp);
            else if (subcode == 0x4A)
                pps_settings(tp);
            else if (subcode == 0xA0)
                dac_values(tp);
            else if (subcode == 0xA1)
                osc_sense(tp);
            else if (subcode == 0xA2)
                pps_timing_mode(tp);
            else if (subcode == 0xA5)
                packet_mask(tp);
            else if (subcode == 0xA7)
                sat_solutions(tp);   // not on ThunderBolt-E
            else if (subcode == 0xA8)
                discipline_params(tp);
            else if (subcode == 0xA9)
                survey_params(tp);
            else if (subcode == 0xAB)
                primary_timing(tp);
            else if (subcode == 0xAC)
                secondary_timing(tp);
            else
                unknown_msg(tp);
        }



        private void single_ecef_fix(TsipPacket tp)
        {
            Debug.Print("Packet 42 (XYZ ECEF):");

            Single x = tp.GetNextSingle();
            Single y = tp.GetNextSingle();
            Single z = tp.GetNextSingle();
            Single time_of_fix = tp.GetNextSingle();
        }

        private void velocity_fix(TsipPacket tp)
        {
            Debug.Print("Packet 43 (XYZ ECEF velocity):");

            Single x_vel = tp.GetNextSingle();
            Single y_vel = tp.GetNextSingle();
            Single z_vel = tp.GetNextSingle();
            Single bias_rate = tp.GetNextSingle();
            Single time_of_fix = tp.GetNextSingle();
        }

        private void get_alm_health(TsipPacket tp)
        {
            Debug.Print(":0x49   (Almanac Health Page Report)");
            for (int i = 0; i < 32; i++)
                Satellites[i].IsHealthy = (tp.GetNextByte() == 0);
        }

        private void io_options(TsipPacket tp)
        {
            Debug.Print("Packet 55 (I/O options):");

            byte posn = tp.GetNextByte();
            byte vel = tp.GetNextByte();
            byte timing = tp.GetNextByte();
            byte aux = tp.GetNextByte();
            _levelType = ((aux & 0x08) == 0x08) ? "dB" : "AMU";
        }

        private void enu_velocity_fix(TsipPacket tp)
        {
            Debug.Print("Packet 56 (ENU velocity):");

            Single x_vel = tp.GetNextSingle();
            Single y_vel = tp.GetNextSingle();
            Single z_vel = tp.GetNextSingle();
            Single bias_rate = tp.GetNextSingle();
            Single time_of_fix = tp.GetNextSingle();
        }

        private void last_fix_info(TsipPacket tp)
        {
            Debug.Print("Packet 57 (last fix info):");

            byte source_of_fix = tp.GetNextByte();
            byte tracking_mode = tp.GetNextByte();
            Single time_of_fix = tp.GetNextSingle();
            UInt16 week_of_fix = tp.GetNextWord();
        }

        private void packet_58(TsipPacket tp)
        {
            Debug.Print("Packet 58 (GPS system data):");

            byte op = tp.GetNextByte();
            byte type = tp.GetNextByte();
            byte prn = tp.GetNextByte();
            byte len = tp.GetNextByte();
        }

        /// <summary>
        /// Satellite Health Report
        /// 
        /// op = 0x03:
        /// The 32 single-byte flags (byte 1-32) identify the Enable/Disable attribute
        /// status for the 32 satellites. Disabled satellites are not used, even when
        /// the satellite is in good health. The flags identify any satellites which are
        /// manually disabled by user. The factory default setting is to enable all
        /// satellites for inclusion in position solution computations if they are in
        /// good health and conform with the mask values for elevation angle,
        /// signal level, PDOP, and PDOP Switch
        ///
        /// op = 0x06:
        /// The 32 single-byte flags (byte 1-32) identify the Heed/Ignore Health
        /// attribute status for the 32 satellites. Flags with the Ignore attribute set
        /// indicate that the satellite can be used in the position solution, regardless
        /// of whether the satellite is in good or bad health. The factory default
        /// setting is to heed satellite health when choosing the satellites included
        /// in a position solution computation
        /// </summary>
        private void sat_health(TsipPacket tp)
        {
            Debug.Print(":0x59 (Satellite Attribute Database Status Report)");

            byte op = tp.GetNextByte();
            if (op == 3)        // enable / disable
            {
                for (int i = 0; i < 32; i++)
                    Satellites[i].Disabled = (tp.GetNextByte() == 1);
            }
            else if (op == 6) // heed / ignore
            {
                for (int i = 0; i < 32; i++)
                    Satellites[i].ForcedHealthy = (tp.GetNextByte() == 1);
            }
            else
                unknown_msg(tp);
        }

        private void raw_data(TsipPacket tp)
        {

            byte prn = tp.GetNextByte();
            prn--;
            if (prn > 31)
            {
                unknown_msg(tp);
                return;
            }

            //Debug.Print(":0x5A    (Raw Measurement Data prn = {0} )", prn);

            Satellites[prn].SampleLength = tp.GetNextSingle();
            Satellites[prn].SignalLevel = tp.GetNextSingle();
            Satellites[prn].CodePhase = tp.GetNextSingle();
            Satellites[prn].Doppler = tp.GetNextSingle();
            Satellites[prn].RawTime = tp.GetNextDouble();
            Satellites[prn].Tracked = true;
        }

        private void eph_status(TsipPacket tp)
        {
            Debug.Print("Packet 5B (Sat ephemeris status):");

            byte prn = tp.GetNextByte();
            prn--;
            if (prn > 31)
            {
                unknown_msg(tp);
                return;
            }

            Satellites[prn].CollectionTime = tp.GetNextSingle();
            Satellites[prn].EphemerisHealth = tp.GetNextByte();
            Satellites[prn].IODE = tp.GetNextByte();
            Satellites[prn].Toe = tp.GetNextSingle();
            Satellites[prn].FitIntervalFlag = tp.GetNextByte();
            Satellites[prn].UserRangeAccuracy = tp.GetNextSingle();
        }

        private void sat_tracking(TsipPacket tp)
        {
            Debug.Print(":0x5C    (Satellite Tracking)");

            byte prn = tp.GetNextByte();
            prn--;
            if (prn > 31)
            {
                unknown_msg(tp);
                return;
            }

            Satellites[prn].Slot = Satellites[prn].Channel = tp.GetNextByte();
            Satellites[prn].Slot &= 0x07;
            Satellites[prn].Channel >>= 3;
            Satellites[prn].AcquisitionFlag = tp.GetNextByte();
            Satellites[prn].EphemerisFlag = tp.GetNextByte();
            Satellites[prn].SignalLevel = tp.GetNextSingle();
            Satellites[prn].TimeOfWeek = tp.GetNextSingle();
            Satellites[prn].Elevation = tp.GetNextSingle();
            Satellites[prn].Azimuth = tp.GetNextSingle();
            Satellites[prn].Age = tp.GetNextByte();
            Satellites[prn].MsecStatus = tp.GetNextByte();
            Satellites[prn].BadDataFlag = tp.GetNextByte();
            Satellites[prn].CollectingData = tp.GetNextByte();
        }

        private void eeprom_status(TsipPacket tp)
        {
            Debug.Print("Packet 5F (EEPROM status):");

            byte flag = tp.GetNextByte();
            if (flag == 0x11)
            {
                UInt16 ee_status = tp.GetNextWord();
            }
        }

        private void filter_config(TsipPacket tp)
        {
            Debug.Print("Packet 70 (Filter config):");

            pv_filter = tp.GetNextByte();
            static_filter = tp.GetNextByte();
            altitude_filter = tp.GetNextByte();
            kalman_filter = tp.GetNextByte();
        }

        private void ecef_fix(TsipPacket tp)
        {
            Debug.Print("Packet 83 (XYZ ECEF):");

            Double x = tp.GetNextDouble();
            Double y = tp.GetNextDouble();
            Double z = tp.GetNextDouble();
            Double clock_bias = tp.GetNextDouble();
            Single time_of_fix = tp.GetNextSingle();
        }

        private void raise_position_change()
        {
            if (last_position != null || _currentPosition.Equals(last_position) == false)
                if (PositionChanged != null)
                    PositionChanged(this, new EventArgs()); // raise Position Changed event

            last_position = _currentPosition;

        }

        private void lla_fix(TsipPacket tp)
        {
            Debug.Print("Packet 84 (LLA fix):");

            _currentPosition.Latitude = tp.GetNextDouble();
            _currentPosition.Longitude = tp.GetNextDouble();
            _currentPosition.Altitude = tp.GetNextDouble();

            Double clock_bias = tp.GetNextDouble();
            Single time_of_fix = tp.GetNextSingle();

            raise_position_change();
        }

        private void rcvr_config(TsipPacket tp)
        {

            Debug.Print("0xBB (Receiver config)");

            byte subcode = tp.GetNextByte();
            byte rcvr_mode = tp.GetNextByte();
            byte rsvd1 = tp.GetNextByte();
            byte dynamics_code = tp.GetNextByte();
            byte rsvd2 = tp.GetNextByte();
            Single el_mask = tp.GetNextSingle();
            Single amu_mask = tp.GetNextSingle();
            Single pdop_mask = tp.GetNextSingle();
            Single pdop_switch = tp.GetNextSingle();
            byte rsvd3 = tp.GetNextByte();
            byte foliage_mode = tp.GetNextByte();
        }

        #endregion

        #region TSIP transmit primatives

        private void send_byte(byte[] val)
        {
            foreach (byte b in val)
            {
                send_byte(b);
            }

        }

        private void send_byte(byte val)
        {
            m_port.Write(new byte[] { val }, 0, 1);
            if (val == (byte)TsipControlBytes.DLE) // DLE needs to be sent twice
                m_port.Write(new byte[] { val }, 0, 1);
        }

        private void send_word(UInt16 val)
        {
            byte[] buffer = BitConverter.GetBytes(val);
            send_byte(buffer[1]);
            send_byte(buffer[0]);
        }

        private void send_dword(double val)
        {
            byte[] buffer = BitConverter.GetBytes(val);
            send_byte(buffer);
            //for (int i = 3; i >= 0; i--)
            //    send_byte(buffer[i]);
        }

        private void send_uint(uint val)
        {
            byte[] buffer = BitConverter.GetBytes(val);
            Debug.Print(buffer.ToString());
            send_byte(buffer);
        }

        private void send_single(float val)
        {
            byte[] buffer = BitConverter.GetBytes(val);
            send_byte(buffer); 
            //for (int i = 3; i >= 0; i--)
            //    send_byte(buffer[i]);
        }

        private void send_double(double val)
        {
            byte[] buffer = BitConverter.GetBytes(val);
            send_byte(buffer);
            //for (int i = 7; i >= 0; i--)
            //    send_byte(buffer[i]);
        }

        private void send_msg_start(byte id)
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)TsipControlBytes.DLE;
            buffer[1] = id;
            m_port.Write(buffer, 0, 2);
        }

        private void send_msg_end()
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)TsipControlBytes.DLE;
            buffer[1] = (byte)TsipControlBytes.ETX;
            m_port.Write(buffer, 0, 2);
        }

        #endregion

        #region TSIP request and option setting packets

        #region 0x1A - RTCM Wrapper requests (not supported)
        // Allows the GPS receiver to accept RTCM data into the control port.
        #endregion

        #region 0x1C Thunderbolt E Version Information

        /// <summary>
        /// Obtains the firmware version (Thunderbolt E versions only).  
        /// Version information is available when the FirmwareVersionInfoReceived event is raised.
        /// </summary>
        public void RequestFirmwareVersion()
        {
            if (m_port.IsOpen)
                request_fw_ver();
        }

        private void request_fw_ver()
        {
            Debug.Print("request_fw_ver");
            send_msg_start(0x1C);   //!!! ThunderBolt-E only returned via packet id 0x1C
            send_byte(0x01);
            send_msg_end();
        }

        /// <summary>
        /// Obtains the hardware version  (Thunderbolt E versions only).  
        /// Version information is available when the HardwareVersionInfoReceived event is raised.
        /// </summary>
        public void RequestHardwareVersion()
        {
            if (m_port.IsOpen)
                request_hw_ver();
        }

        private void request_hw_ver()
        {
            Debug.Print("request_hw_ver");
            send_msg_start(0x1C);   //!!! ThunderBolt-E only returned via packet id 0x1C
            send_byte(0x03);
            send_msg_end();
        }

        private void ebolt_version(TsipPacket tp) // Thunderbolt E Version Information
        {
            byte rev_month;
            byte rev_day;
            UInt16 rev_year;

            byte subcode = tp.GetNextByte();
            if (subcode == 0x81) // firmware version
            {
                Debug.Print(":0x1C.0x81 (Firmware Version)");
                byte reserved8 = tp.GetNextByte();
                byte major = tp.GetNextByte(); // major version number
                byte minor = tp.GetNextByte(); // minor version number
                byte build = tp.GetNextByte(); // build number
                rev_month = tp.GetNextByte();  // build month
                rev_day = tp.GetNextByte();    // build day
                rev_year = tp.GetNextWord();   // build year
                _firmwareVersion.VersionString = tp.GetNextString(); // product name string
                try
                {
                    _firmwareVersion.Date = new DateTime(rev_year, rev_month, rev_day, 0, 0, 0);
                }
                catch (Exception e)
                {
                    _firmwareVersion.Date = new DateTime(0L);   // bad date
                    Debug.Print("Date Conversion Exception: " + e.Message);
                }
                _firmwareVersion.Code = 0;
                if (FirmwareVersionInfoReceived != null)
                    FirmwareVersionInfoReceived(this, new VersionInfoEventArgs(_firmwareVersion));
            }
            else if (subcode == 0x83) // hardware version
            {
                Debug.Print(":0x1C.0x83 (Hardware Version)");
                UInt32 serno = tp.GetNextDWord();    // board serial number
                rev_day = tp.GetNextByte();          // board build day
                rev_month = tp.GetNextByte();        // board build month
                rev_year = tp.GetNextWord();         // board build year
                byte rev_hour = tp.GetNextByte();    // board build hour

                _hardwareVersion.Code = tp.GetNextWord(); // hardware code associated with hardware ID
                _hardwareVersion.VersionString = tp.GetNextString(); // Hardware ID
                _hardwareVersion.SerialNumber = "SN:" + serno.ToString();

                try
                {
                    _hardwareVersion.Date = new DateTime(rev_year, rev_month, rev_day, rev_hour, 0, 0);
                }
                catch (Exception e)
                {
                    _hardwareVersion.Date = new DateTime(0L);  // bad date
                    Debug.Print("Date Conversion Exception: " + e.Message);
                }
                if (HardwareVersionInfoReceived != null)
                    HardwareVersionInfoReceived(this, new VersionInfoEventArgs(_hardwareVersion));
            }
            else
                unknown_msg(tp);
        }

        #endregion

        #region 0x1D Oscillator Offset Command (not supported)
        /// <summary>
        /// The GPS oscillator offset requires clearing only when servicing the receiver or performing
        /// field diagnostics. To clear the oscillator offset in the receiver, the receiver is sent one data
        /// byte, the ASCII letter C (C = 0x43) as shown in Table 2-3. Clear the oscillator only when
        /// specifically told to do so by an authorized Trimble service center.
        /// </summary>
        private void request_gps_oscillator_clear()
        {
            //    Debug.Print("request_gps_oscillator_clear");
            //    send_msg_start(0x1D);   
            //    send_msg_end();
        }
        #endregion

        #region 0x1E & 0x25 Resets

        /// <summary>
        /// A warm reset clears ephemeris and oscillator uncertainty but
        /// retains the last position, time and almanac.
        /// </summary>
        public void WarmReset()
        {
            if (m_port.IsOpen)
                request_warm_reset();
        }

        private void request_warm_reset()
        {
            Debug.Print("Request Warm Reset");
            send_msg_start(0x1E);  // initiate cold, warm or factory reset
            send_byte(0x0E);
            send_msg_end();
        }

        /// <summary>
        /// A cold reset will clear the GPS data (almanac, ephemeris, etc.)
        /// It is the equivalent of a power reset.
        /// </summary>
        public void ColdReset()
        {
            if (m_port.IsOpen)
                request_cold_reset();
        }

        private void request_cold_reset()
        {
            Debug.Print("Request Cold Reset");
            send_msg_start(0x1E);  // initiate cold, warm or factory reset
            send_byte(0x4B);
            send_msg_end();
        }

        /// <summary>
        /// This packet commands the GPS receiver to perform a hot reset. This is not equivalent 
        /// to cycling the power; RAM is not cleared. 
        /// </summary>
        public void HotReset()
        {
            if (m_port.IsOpen)
                request_hot_reset();
        }

        private void request_hot_reset()
        {
            Debug.Print("request_hot_reset");
            send_msg_start(0x25);
            send_msg_end();
        }

        /// <summary>
        /// A factory reset will clear all GPS data and restore the factory defaults
        /// of all configuration parameters stored in flash.
        /// </summary>
        public void FactoryReset()
        {
            if (m_port.IsOpen)
                request_factory_reset();
        }

        private void request_factory_reset()
        {
            Debug.Print("Request Factory Reset");
            send_msg_start(0x1E);
            send_byte(0x46);
            send_msg_end();
        }

        #endregion

        #region 0x1F Request Software Version

        /// <summary>
        /// This requests information about the version of software in Thunderbolt
        /// Use the SoftwareVersionInfoReceived event to obtain results.
        /// </summary>
        public void RequestSoftwareVersion()
        {
            if (m_port.IsOpen)
                request_software_version();
        }

        private void request_software_version()
        {
            Debug.Print("Request Software Version"); // response in packet id 0x45
            send_msg_start(0x1F);
            send_msg_end();
        }

        private void version_info(TsipPacket tp)
        {
            Debug.Print(":0x45 (Software Version Info Received)");

            byte ap_major = tp.GetNextByte();  // Application firmware
            byte ap_minor = tp.GetNextByte();
            byte ap_month = tp.GetNextByte();
            byte ap_day = tp.GetNextByte();
            UInt16 ap_year = (ushort)(tp.GetNextByte() + 2000); // docs say 1900 based!

            byte core_major = tp.GetNextByte(); // GPS firmware
            byte core_minor = tp.GetNextByte();
            byte core_month = tp.GetNextByte();
            byte core_day = tp.GetNextByte();
            UInt16 core_year = (ushort)(tp.GetNextByte() + 2000); // docs say 1900 based!

            try
            {
                _firmwareVersion.Major = ap_month;
                _firmwareVersion.Minor = ap_minor;
                _firmwareVersion.Date = new DateTime(ap_year, ap_month, ap_day);

                _hardwareVersion.Major = core_major;
                _hardwareVersion.Minor = core_minor;
                _hardwareVersion.Date = new DateTime(core_year, core_month, core_day);

                if (SoftwareVersionInfoReceived != null)
                    SoftwareVersionInfoReceived(this, new VersionInfoEventArgs(_firmwareVersion));

                if (HardwareVersionInfoReceived != null)
                    HardwareVersionInfoReceived(this, new VersionInfoEventArgs(_hardwareVersion));
            }
            catch (Exception e)
            {
                Debug.Print("Exception:" + e.Message);
            }
        }

        #endregion

        #region 0x20 Request Satellite Almanac (Not Supported)
        /// <summary>
        /// Requests almanac data for one satellite
        /// </summary>
        /// <param name="prn">Satellite PRN number.</param>
        //public void RequestAlmanac(byte prn)
        //{
        //    if (m_port.IsOpen)
        //        request_almanac(prn);
        //}

        //private void request_almanac(byte prn)
        //{
        //    Debug.Print("request_almanac"); // returned in packet id 0x40
        //    send_msg_start(0x20);
        //    send_byte(prn);
        //    send_msg_end();
        //}

        #endregion

        #region 0x21 Request Current GPS Time

        /// <summary>
        /// This requests requests current GPS time. 
        /// The requested data is returned via the GPSTimeReceived event.
        /// 
        /// GPS time differs from UTC by a variable integral number of seconds. 
        /// UTC = (GPS time) – (GPS/UTC offset).
        /// The GPS week number reference is Week # 0 beginning on January 6, 1980. 
        /// The seconds count begins with 0 each Sunday morning at midnight GPS time. 
        /// A negative value for TOW (time of week) indicates that the time is not yet known
        /// </summary>
        public void RequestGPSTime()
        {
            if (m_port.IsOpen)
                request_gps_time();
        }

        private void request_gps_time()
        {
            Debug.Print("Request GPS Time");
            send_msg_start(0x21); // data is returned with packet id 0x41
            send_msg_end();
        }

        private void gps_time_received(TsipPacket tp)
        {
            Debug.Print(":0x41 GPS Time Received");
            Single s_time = tp.GetNextSingle();
            Int16 s_week = (short)tp.GetNextWord();
            Single s_offset = tp.GetNextSingle();
            if (GpsTimeReceived != null)
                GpsTimeReceived(this, new GPSTimeInfoEventArgs(s_time, s_week, s_offset));
        }

        #endregion

        #region 0x24 Request Satellite List

        /// <summary>
        /// This requests a list of satellites used for the current position/time fix. 
        /// The requested data is returned via the SatelliteListReceived event.
        /// </summary>
        public void RequestSatelliteList()
        {
            if (m_port.IsOpen)
                request_sat_list();
        }

        private void request_sat_list()
        {
            Debug.Print("Request Satellite List");
            send_msg_start(0x24); // data is returned with packet id 0x6D
            send_msg_end();
        }

        /// <summary>
        /// This packet provides a list of satellites used for position or time-only fixes by the GPS
        /// receiver. The packet also provides the dilution of precision values PDOP, HDOP, VDOP and TDOP 
        /// of that set and provides the current mode (automatic or manual, 3-D or 2-D, Over-Determined Clock
        /// mode, etc.). This packet has variable length equal to (17 + nsvs) where "nsvs" is the
        /// number of satellites used in the solution. If an SV is rejected for use by the T-RAIM
        /// algorithm then the SV PRN value will be negative.
        /// 
        /// PDOP = positional DOP
        /// HDOP = horizontal DOP
        /// VDOP = vertical DOP
        /// TDOP = temporal DOP
        /// 
        /// Note: The GPS receiver sends this packet in response to packet 0x24 or automatically. 
        /// </summary>
        private void sat_list(TsipPacket tp)
        {

            byte mode, count, dimension;

            dimension = mode = count = tp.GetNextByte();
            dimension &= 0x07;  // fix dimension is in first 3 bits
            mode &= 0x08;       // mode is in bit 3
            mode >>= 3;
            count >>= 4;        // tracked sat count in upper 4 bits

            //Debug.Print(":0x6D    (Satellite List count = {0})", count);

            positional_dop = Helpers.FloatToFixPrecision(tp.GetNextSingle());
            horizontal_dop = Helpers.FloatToFixPrecision(tp.GetNextSingle());
            vertical_dop = Helpers.FloatToFixPrecision(tp.GetNextSingle());
            temporal_dop = Helpers.FloatToFixPrecision(tp.GetNextSingle());
            fix_dimension = Helpers.ByteToFixDimension(dimension);
            fix_mode = Helpers.ByteToFixMode(mode);

            for (int i = 0; i < 32; i++)    // clear current tracking flags
                Satellites[i].UsedInFix = false;

            for (int i = 0; i < count; i++)
            {
                bool used = true;
                byte prn = tp.GetNextByte();
                if ((prn & 0x80) == 0x80)   // satellite is tracked but is not used in fix
                {
                    used = false;
                    prn &= 0x7F;
                }
                prn--;
                if (prn > 31)
                    continue;               // disregard bogus data

                Satellites[prn].Tracked = true;
                Satellites[prn].UsedInFix = used;
            }
        }

        #endregion

        #region 0x26 Request Receiver Health

        /// <summary>
        /// Provides information about the satellite tracking status and the operational health 
        /// of the receiver. The receiver sends this packet after power-on or software-initiated 
        /// resets, in response to Command Packet 0x26 (E version only), during an update cycle, 
        /// when a new satellite selection is attempted, and when the receiver detects a change 
        /// in its health.
        /// </summary>
        public void RequestReceiverHealth()
        {
            if (m_port.IsOpen)
                request_rcvr_health();
        }

        private void request_rcvr_health()
        {
            Debug.Print("request_rcvr_health");
            send_msg_start(0x26);   //!!! ThunderBolt-E only 
            send_msg_end();
        }

        private void ebolt_health1(TsipPacket tp)
        {
            Debug.Print(":0x46 (E Receiver Health 1st Packet):");
            receiver_status = (ReceiverStatus)tp.GetNextByte();
            //byte sv_fix = tsip_byte();
            byte antenna_fault = tp.GetNextByte();
        }

        private void ebolt_health2(TsipPacket tp)
        {
            Debug.Print(":0x4B (E Receiver Health 2nd Packet):");
            byte id = tp.GetNextByte();
            byte rtc = tp.GetNextByte();
            byte superpackets = tp.GetNextByte();
        }

        #endregion

        private void write_all_nvs()
        {
            Debug.Print("write_all_nvs");
            send_msg_start(0x8E);   //!!! ThunderBolt-E only
            send_byte(0x26);
            send_msg_end();
        }

        #region Satellite Signal Levels

        public void RequestSatelliteSignalLevels()
        {
            if (m_port.IsOpen)
                request_sig_levels();
        }

        private void request_sig_levels()
        {
            Debug.Print("Request Satellite Signal Levels");
            send_msg_start(0x27);
            send_msg_end();
        }

        private void clear_sat_tracking()
        {
            for (int i = 0; i < 32; i++)
                Satellites[i].Tracked = false;
        }


        private void receive_sig_levels(TsipPacket tp)
        {
            Debug.Print(":0x47    (Signal Levels for All Tracked Satellites Report)");

            byte count = tp.GetNextByte();
            for (int i = 0; i < count; i++)
            {
                byte prn = tp.GetNextByte();
                Single sig_level = tp.GetNextSingle();
                prn--;          // zero offset adjust
                if (prn > 31)
                    continue;   // disregard bogus data

                Satellites[prn].SignalLevel = sig_level;
                Satellites[prn].Tracked = true;
            }
        }

        #endregion

        private void request_alm_health()
        {
            Debug.Print("request_alm_health");
            send_msg_start(0x29);
            send_msg_end();
        }

        private void set_xyz(float x, float y, float z)
        {
            Debug.Print("set_xyz");
            send_msg_start(0x31);
            send_single(x);
            send_single(y);
            send_single(z);
            send_msg_end();
        }

        private void set_lla(float lat, float lon, float alt)
        {
            Debug.Print("set_lla");
            send_msg_start(0x32);
            send_single(lat);
            send_single(lon);
            send_single(alt);
            send_msg_end();
        }

        private void set_single_sat(byte prn)
        {
            Debug.Print("set_single_sat");
            send_msg_start(0x34);
            send_byte(prn);
            send_msg_end();
        }

        private void request_io_options()
        {
            Debug.Print("request_io_options");
            send_msg_start(0x35);
            send_msg_end();
        }

        private void set_io_options(byte posn, byte vel, byte timing, byte aux)
        {
            Debug.Print("set_io_options");
            send_msg_start(0x35);
            send_byte(posn);
            send_byte(vel);
            send_byte(timing);
            send_byte(aux);
            send_msg_end();
        }

        private void request_last_posn()
        {
            Debug.Print("request_last_posn");
            send_msg_start(0x37);
            send_msg_end();
        }

        private void request_system_data(byte mode, byte prn)
        {
            Debug.Print("request_system_data");
            send_msg_start(0x38);
            send_byte(0x01);
            send_byte(mode);
            send_byte(prn);
            send_msg_end();
        }

        private void twiddle_health(byte mode, byte prn)
        {
            Debug.Print("twiddle_health");
            send_msg_start(0x39);
            send_byte(mode);
            send_byte(prn);
            send_msg_end();
        }

        private void request_sat_health()
        {
            Debug.Print("request_sat_health");
            twiddle_health(3, 0x00);  // request enable/disable status of all sats
            twiddle_health(6, 0x00);  // request heed/ignore status of all sats
        }

        private void request_last_raw(byte prn)
        {
            Debug.Print("Request Last Raw");
            send_msg_start(0x3A);
            send_byte(prn);
            send_msg_end();
        }

        private void request_eph_status(byte prn)
        {
            Debug.Print("request_eph_status");
            send_msg_start(0x3B);
            send_byte(prn);
            send_msg_end();
        }

        /// <summary>
        /// Request currently tracked satellite status. The receiver acknowledges 
        /// with Report Packet 0x5C when data is available. 
        /// </summary>
        public void RequestTrackedSatelliteStatus()
        {
            request_sat_status(0x00); // 0x00 == all tracked sats
        }

        private void request_sat_status(byte prn)
        {
            //Debug.Print("request_sat_status ({0}) ", prn);
            send_msg_start(0x3C);
            send_byte(prn);
            send_msg_end();
        }

        private void request_eeprom_status()
        {
            Debug.Print("request_eeprom_status");
            send_msg_start(0x3F);
            send_byte(0x11);
            send_msg_end();
        }

        private void request_filter_config()
        {
            Debug.Print("request_filter_config");
            send_msg_start(0x70);
            send_msg_end();
        }

        private void set_filter_config(byte pv, byte stat, byte alt, byte kalman)
        {
            Debug.Print("set_filter_config");
            send_msg_start(0x70);
            send_byte(pv);
            send_byte(stat);
            send_byte(alt);
            send_byte(kalman); // rsvd on ThunderBolt,  kalman on ThunderBolt-E
            send_msg_end();
        }

        private void request_rcvr_config()
        {
            Debug.Print("request_rcvr_config");
            send_msg_start(0xBB);
            send_byte(0x00);
            send_msg_end();
        }

        private void set_rcvr_config()
        {
            //$10,$BB,$00,RXMode,$00,Dynamics,$00,$3E,$33,$33,$33,$40,$80,$00,$00,$41,$00,$00,$00,$40,$C0,$00,$00,$00,Foilage,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$10,$03
            //$10,$BB,$00,RXMode,$00,Dynamics,$00,$3E,$33,$33,$33,$40,$80,$00,$00,$41,$00,$00,$00,$40,$C0,$00,$00,$00,Foilage,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$00,$10,$03

            Debug.Print("set_rcvr_config");
            send_msg_start(0xBB);
            send_byte(new byte[] 
            {
                0, // Primary receiver configuration data
                (byte)_receiverMode, // Receiver Mode
                0,                  // Value is ignored
                4,                  // Dynamics Code (Stationary)
                0,                  // Value is ignored
                0x3E, 0x32, 0xB8, 0xC3,      // Lowest satellite elevation for fixes (radians)
                40,80,0,0,41,       // Minimum signal level for fixes
                0,0,0,40,           // Maximum DOP for fixes
                0xC0, 0, 0, 0xFF,         // Switches 2D/3D mode
                1,                  // Foliage Mode (Sometimes)
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0  // Values are ignored
            });
            send_msg_end();
        }

        private void request_serial_config(byte port)
        {
            Debug.Print("request_serial_config");
            send_msg_start(0xBC);
            send_byte(port);
            send_msg_end();
        }

        private void set_serial_config()
        {
            //!!!
            Debug.Print("set_serial_config");
        }

        private void request_datum()
        {
            Debug.Print("request_datum");
            send_msg_start(0x8E);
            send_byte(0x15);
            send_msg_end();
        }

        private void request_manuf_params()
        {
            Debug.Print("request_manuf_params");
            send_msg_start(0x8E);
            send_byte(0x41);
            send_msg_end();
        }

        private void request_prodn_params()
        {
            Debug.Print("request_prodn_params");
            send_msg_start(0x8E);
            send_byte(0x42);
            send_msg_end();
        }

        private void revert_segment(byte segment)
        {
            Debug.Print("revert_segment");
            send_msg_start(0x8E);
            send_byte(0x45);
            send_byte(segment);
            send_msg_end();
        }

        private void request_pps()
        {
            Debug.Print("request_pps");
            send_msg_start(0x8E);
            send_byte(0x4A);
            send_msg_end();
        }

        private void set_pps(byte pps_enable, byte pps_polarity, double cable_delay, float threshold)
        {
            Debug.Print("set_pps");
            send_msg_start(0x8E);
            send_byte(0x4A);
            send_byte(pps_enable);
            send_byte(0x00);
            send_byte(pps_polarity);
            send_double(cable_delay);
            send_single(threshold);
            send_msg_end();
        }

        private void save_segment(byte segment)
        {
            Debug.Print("save_segment");
            send_msg_start(0x8E);
            send_byte(0x4C);
            send_byte(segment);
            send_msg_end();
        }

        private void request_dac_voltage()
        {
            Debug.Print("request_dac_voltage");
            send_msg_start(0x8E);
            send_byte(0xA0);
            send_msg_end();
        }

        private void set_dac_voltage(float volts)
        {
            Debug.Print("set_dac_voltage");
            send_msg_start(0x8E);
            send_byte(0xA0);
            send_byte(0x00);
            send_single(volts);
            send_msg_end();
        }

        private void set_dac_value(UInt32 value)
        {
            Debug.Print("set_dac_value");
            send_msg_start(0x8E);
            send_byte(0xA0);
            send_byte(0x01);
            send_dword(value);
            send_msg_end();
        }

        private void request_osc_sense()
        {
            Debug.Print("request_osc_sense");
            send_msg_start(0x8E);  // not available on ThunderBolt-E or early ThunderBolts
            send_byte(0xA1);
            send_msg_end();
        }

        private void set_osc_sense(byte mode)
        {
            Debug.Print("set_osc_sense");
            send_msg_start(0x8E);  // not available on ThunderBolt-E or early ThunderBolts
            send_byte(0xA1);
            send_byte(mode);
            send_msg_end();
        }

        #region Receiver Time Mode (UTC or GPS)

        private TimingModes time_mode = TimingModes.UTC;
        /// <summary>
        /// Gets or sets receiver timing mode (GPS or UTC)
        /// </summary>
        public TimingModes TimingMode
        {
            get { return time_mode; }
            set
            {
                time_mode = value;
                set_timing_mode();
            }
        }

        private void request_timing_mode()
        {
            Debug.Print("request_timing_mode");
            send_msg_start(0x8E);
            send_byte(0xA2);
            send_msg_end();
        }

        private void set_timing_mode()
        {
            Debug.Print("Set Timing Mode");
            send_msg_start(0x8E);
            send_byte(0xA2);
            send_byte((byte)(time_mode == TimingModes.UTC ? 0x03 : 0x00));
            send_msg_end();
        }

        #endregion

        private void set_discipline_mode(byte mode)
        {
            Debug.Print("set_discipline_mode");
            send_msg_start(0x8E);
            send_byte(0xA3);
            send_byte(mode);
            send_msg_end();
        }

        private void exit_test_mode()
        {
            Debug.Print("exit_test_mode");
            send_msg_start(0x8E);
            send_byte(0xA4);
            send_byte(0x00);
            send_msg_end();
        }

        private void set_test_mode()
        {
            //!!!
            Debug.Print("set_test_mode");
        }

        private void request_packet_mask()
        {
            Debug.Print("request_packet_mask");
            send_msg_start(0x8E);
            send_byte(0xA5);
            send_msg_end();
        }

        private void set_packet_mask(UInt16 mask1, UInt16 mask2)
        {
            Debug.Print("set_packet_mask");
            send_msg_start(0x8E);
            send_byte(0xA5);
            send_word(mask1);
            send_word(mask2);
            send_msg_end();
        }

        public void start_self_survey()
        {
            Debug.Print("start_self_survey");
            send_msg_start(0x8E);
            send_byte(0xA6);
            send_byte(0x00);
            send_msg_end();
        }

        private void request_discipline_params(byte type)
        {
            Debug.Print("request_disipline_params");
            send_msg_start(0x8E);
            send_byte(0xA8);
            send_byte(type);
            send_msg_end();
        }

        private void request_all_dis_params()
        {
            Debug.Print("request_all_dis_params");
            request_discipline_params(0x00);
            request_discipline_params(0x01);
            request_discipline_params(0x02);
            request_discipline_params(0x03);
        }

        private void set_discipline_params()
        {
            Debug.Print("set_discipline_params");
            send_msg_start(0x8E);
            send_byte(0xA8);
            send_byte(0x00);
            send_single(_timeConstant);
            send_single(_dampingFactor);
            send_msg_end();

            send_msg_start(0x8E);
            send_byte(0xA8);
            send_byte(0x01);
            send_single(_oscGain);
            send_single(_minVolts);
            send_single(_maxVolts);
            send_msg_end();

            send_msg_start(0x8E);
            send_byte(0xA8);
            send_byte(0x02);
            send_single(_jamSync);
            send_single(_maximumFrequencyOffset);
            send_msg_end();

            send_msg_start(0x8E);
            send_byte(0xA8);
            send_byte(0x03);
            send_single(_initialVoltage);
            send_msg_end();
        }

        private void request_survey_params()
        {
            Debug.Print("request_survey_params");
            send_msg_start(0x8E);
            send_byte(0xA9);
            send_msg_end();
        }

        public void set_survey_params(byte enable_survey, byte save_survey, UInt32 survey_len)
        {
            Debug.Print("set_survey_params");
            send_msg_start(0x8E);
            send_byte(0xA9);
            send_byte(enable_survey);
            send_byte(save_survey);
            //send_dword(survey_len);
            //send_dword(0L);
            send_byte(new byte[] { 00, 00, 00, 64, 00, 00, 00, 00 });
            send_msg_end();

        }

        private void request_primary_timing()
        {
            Debug.Print("request_primary_timing");
            send_msg_start(0x8E);
            send_byte(0xAB);
            send_msg_end();
        }

        private void request_secondary_timing()
        {
            Debug.Print("request_secondary_timing");
            send_msg_start(0x8E);
            send_byte(0xAC);
            send_msg_end();
        }

        #endregion
    }
}
