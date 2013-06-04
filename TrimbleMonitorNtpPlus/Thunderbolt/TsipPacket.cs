using System;
using Microsoft.SPOT;
using System.Text;


namespace TrimbleMonitorNtpPlus.Thunderbolt
{
    public enum TsipControlBytes : byte
    {
        DLE = 0x10,     // TSIP message start code and byte stuffing escape value
        ETX = 0x03      // TSIP message end code
    }

    public enum TsipParserStatus
    {
        Empty,
        Full,
        Data,
        DLE1,
        DLE2
    }

    /// <summary>
    /// Handles TSIP packet data accumulation, packet parsing, low-level validation and 
    /// provides primatives for removing sequential values from the packet data buffer.
    /// </summary>
    class TsipPacket
    {
        private const short MaxRptbuf = 256;

        private TsipParserStatus _status;	// TSIP packet format/parse status 
        private short _length;				// received byte count < MAX_RPTBUF 
        private short _counter;				// counter for data retrieval functions
        private readonly byte[] _packetData;	        // TSIP data packet

        public TsipPacket()
        {
            _counter = 0;
            _length = 0;
            _status = TsipParserStatus.Empty;
            PacketType = 0;
            _packetData = new byte[MaxRptbuf];
        }

        public static string StringFormat(string baseString, object[] objectArray)
        {
            var sb = new StringBuilder(baseString);
            for (var i = 0; i < objectArray.Length; i++)
                sb.Replace("{" + i + "}", objectArray[i].ToString());
            return sb.ToString();
        }

        /// <summary>
        /// Returns a readable string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var st = StringFormat("TSIP PACKET: Status={2} Code=0x{1:X2} Length={0} Counter={3} Buffer=:", new object[] { _length, PacketType, _status, _counter });

            for (var x = 0; x < _length; x++)
                st += this._packetData[x].ToString("X2");
            return st;
        }

        /// <summary>
        /// Returns the TSIP packet type (aka report code ... see TSIP docs) 
        /// </summary>
        public byte PacketType { get; private set; }

        /// <summary>
        /// Returns true when a complete TSIP packet has been parsed.
        /// </summary>
        public bool IsComplete
        {
            get { return (this._status == TsipParserStatus.Full); }
        }

        /// <summary>
        /// Retrieves the next byte from the packet buffer.
        /// </summary>
        /// <returns>byte</returns>
        public byte GetNextByte()
        {
            return this._packetData[_counter++];
        }

        /// <summary>
        /// Retrieves the next 2-byte word value from the packet buffer.
        /// </summary>
        /// <returns>word</returns>
        public UInt16 GetNextWord()      // get next two byte (word) field device 
        {
            var buffer = new byte[2];
            if (BitConverter.IsLittleEndian)
            {
                buffer[1] = _packetData[this._counter++];
                buffer[0] = _packetData[this._counter++];
            }
            else
            {
                buffer[0] = _packetData[this._counter++];
                buffer[1] = _packetData[this._counter++];
            }
            return BitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>
        /// Retrieves the next 4-byte DWORD value from the packet buffer.
        /// </summary>
        /// <returns></returns>
        public UInt32 GetNextDWord()
        {
            var buffer = new byte[4];
            for (var i = 3; i >= 0; i--)
                buffer[i] = _packetData[_counter++];
            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Retrieves the next 4-byte (Single) precision value from the packet buffer.
        /// </summary>
        /// <returns>single</returns>
        public Single GetNextSingle()
        {
            var buffer = new byte[4];
            for (var i = 3; i >= 0; i--)
                buffer[i] = _packetData[_counter++];
            return BitConverter.ToSingle(buffer, 0);
        }

        /// <summary>
        /// Retrieves the next 8-byte (Double) precision value from the packet buffer.
        /// </summary>
        /// <returns>double</returns>
        public Double GetNextDouble()
        {
            var buffer = new byte[8];
            for (var i = 7; i >= 0; i--)
                buffer[i] = _packetData[_counter++];
            return BitConverter.ToDouble(buffer, 0);
        }

        /// <summary>
        /// Retrieves the next ASCII string value from the packet buffer. 
        /// </summary>
        /// <returns>string</returns>
        public String GetNextString()
        {
            var asciiString = new StringBuilder();
            byte len = this._packetData[_counter++];               // length of the string
            for (var i = 0; i < len; i++)
                asciiString.Append(this._packetData[_counter++]); // append ascii bytes
            return asciiString.ToString();
        }

        /// <summary>
        /// Accumulates bytes from the receiver, strips control bytes (DLE)
        /// and checks for a valid packet end sequence (DLE ETX).
        /// Note: Use the IsComplete property to determine if a complete packet is available.
        /// </summary>
        public void AddByte(UInt16 inbyte)
        {
            // avoid bogus bytes
            if ((inbyte & (UInt16)0xFF00) != 0)
                return;

            var newbyte = (byte)(inbyte & 0x00FF);
            switch (this._status)
            {
                case TsipParserStatus.DLE1:
                    {
                        switch (newbyte)
                        {
                            case 0:
                            case (byte)TsipControlBytes.ETX:    // illegal TSIP id
                                //Debug.Print("Parse Error: Illegal TSIP id = {0:X}", newbyte);
                                _length = 0;
                                _status = TsipParserStatus.Empty;
                                break;
                            case (byte)TsipControlBytes.DLE:    // try normal message start again
                                _length = 0;
                                _status = TsipParserStatus.DLE1;
                                break;
                            default:                            // legal TSIP ID; start message
                                PacketType = newbyte;
                                _length = 0;
                                _status = TsipParserStatus.Data;
                                break;
                        }
                        break;
                    }
                case TsipParserStatus.Data:
                    {
                        switch (newbyte)
                        {
                            case (byte)TsipControlBytes.DLE: // expect DLE or ETX next
                                _status = TsipParserStatus.DLE2;
                                break;
                            default: // normal data here
                                _packetData[this._length++] = newbyte;
                                break;
                        }
                        break;
                    }
                case TsipParserStatus.DLE2:
                    {
                        switch (newbyte)
                        {
                            case (byte)TsipControlBytes.DLE: // normal data byte
                                _packetData[this._length++] = newbyte;
                                _status = TsipParserStatus.Data;
                                break;
                            case (byte)TsipControlBytes.ETX: // end of message
                                _status = TsipParserStatus.Full;
                                break;
                            default: // error: treat as DLE1; start a new report packet
                                Debug.Print("Parse Error: Treat as DLE1");
                                PacketType = newbyte;
                                _length = 0;
                                _status = TsipParserStatus.Data;
                                break;
                        }
                        break;
                    }
                case TsipParserStatus.Full:
                case TsipParserStatus.Empty:
                default:
                    {
                        switch (newbyte)
                        {
                            case (byte)TsipControlBytes.DLE: // normal message start
                                _length = 0;
                                _status = TsipParserStatus.DLE1;
                                break;
                            default: // error: ignore the new byte
                                //Debug.Print("Parse Error: {1} Ignore new byte {0:X}", newbyte, this.status.ToString());
                                _length = 0;
                                _status = TsipParserStatus.Empty;
                                break;
                        }
                        break;
                    }
            }
            if (_length >= MaxRptbuf)
            {
                //Debug.Print("Parse Error: {1} Buffer Length Exceeded {0}", this.length, this.status.ToString());
                _length = 0;
                _status = TsipParserStatus.Empty;
            }
        }
    }
}
