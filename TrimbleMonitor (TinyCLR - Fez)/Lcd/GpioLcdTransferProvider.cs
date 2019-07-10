// Micro Liquid Crystal Library
// http://microliquidcrystal.codeplex.com
// Appache License Version 2.0 

using System;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace MicroLiquidCrystal
{
    public class GpioLcdTransferProvider : ILcdTransferProvider, IDisposable
    {
        private readonly GpioPin _rsPort;
        private readonly GpioPin _rwPort;
        private readonly GpioPin _enablePort;
        private readonly GpioPin[] _dataPorts;
        private bool _disposed;

        public GpioLcdTransferProvider(int rs, int enable, int d4, int d5, int d6, int d7)
            : this(true, rs, -1, enable, -1, -1, -1, -1, d4, d5, d6, d7)
        { }

        public GpioLcdTransferProvider(int rs, int rw, int enable, int d4, int d5, int d6, int d7)
            : this(true, rs, rw, enable, -1, -1, -1, -1, d4, d5, d6, d7)
        { }

        public GpioLcdTransferProvider(int rs, int enable, int d0, int d1, int d2, int d3, int d4, int d5, int d6, int d7)
            : this(false, rs, -1, enable, d0, d1, d2, d3, d4, d5, d6, d7)
        { }

        public GpioLcdTransferProvider(int rs, int rw, int enable, int d0, int d1, int d2, int d3, int d4, int d5, int d6, int d7)
            : this(false, rs, rw, enable, d0, d1, d2, d3, d4, d5, d6, d7)
        { }

        /// <summary>
        /// Creates a variable of type LiquidCrystal. The display can be controlled using 4 or 8 data lines. If the former, omit the pin numbers for d0 to d3 and leave those lines unconnected. The RW pin can be tied to ground instead of connected to a pin on the Arduino; if so, omit it from this function's parameters. 
        /// </summary>
        /// <param name="fourBitMode"></param>
        /// <param name="rs">The number of the CPU pin that is connected to the RS (register select) pin on the LCD.</param>
        /// <param name="rw">The number of the CPU pin that is connected to the RW (Read/Write) pin on the LCD (optional).</param>
        /// <param name="enable">the number of the CPU pin that is connected to the enable pin on the LCD.</param>
        /// <param name="d0"></param>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <param name="d3"></param>
        /// <param name="d4"></param>
        /// <param name="d5"></param>
        /// <param name="d6"></param>
        /// <param name="d7"></param>
        public GpioLcdTransferProvider(bool fourBitMode, int rs, int rw, int enable, int d0, int d1, int d2, int d3, int d4, int d5, int d6, int d7)
        {
            FourBitMode = fourBitMode;

            _rsPort = GpioController.GetDefault().OpenPin(rs);
            _rsPort.SetDriveMode(GpioPinDriveMode.Output);

            // we can save 1 pin by not using RW. Indicate by passing null instead of pin#
            if (rw > 0)   // (RW is optional)
            {
                _rwPort = GpioController.GetDefault().OpenPin(rw);
                _rwPort.SetDriveMode(GpioPinDriveMode.Output);
            }

            _enablePort = GpioController.GetDefault().OpenPin(enable);
            _enablePort.SetDriveMode(GpioPinDriveMode.Output);

            var dataPins = new[] { d0, d1, d2, d3, d4, d5, d6, d7 };
            _dataPorts = new GpioPin[8];
            for (var i = 0; i < 8; i++)
            {
                if (dataPins[i] > 0)
                {
                    _dataPorts[i] = GpioController.GetDefault().OpenPin(dataPins[i]);
                    _dataPorts[i].SetDriveMode(GpioPinDriveMode.Output);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~GpioLcdTransferProvider()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _rsPort.Dispose();
                _rwPort.Dispose();
                _enablePort.Dispose();

                for (var i = 0; i < 8; i++)
                {
                    if (_dataPorts[i] != null)
                        _dataPorts[i].Dispose();
                }
                _disposed = true;
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        public bool FourBitMode { get; }

        /// <summary>
        /// Write either command or data, with automatic 4/8-bit selection
        /// </summary>
        /// <param name="value">value to write</param>
        /// <param name="mode">Mode for RS (register select) pin.</param>
        /// <param name="backlight">Backlight state.</param>
        public void Send(byte value, bool mode, bool backlight)
        {
            if (_disposed)
                throw new ObjectDisposedException();

            _rsPort.Write(mode ? GpioPinValue.High : GpioPinValue.Low);

            // if there is a RW pin indicated, set it low to Write
            _rwPort?.Write(GpioPinValue.Low);

            if (!FourBitMode)
            {
                Write8Bits(value);
            }
            else
            {
                Write4Bits((byte)(value >> 4));
                Write4Bits(value);
            }
        }

        private void Write8Bits(byte value)
        {
            for (var i = 0; i < 8; i++)
            {
                _dataPorts[i].Write(((value >> i) & 0x01) == 0x01 ? GpioPinValue.High : GpioPinValue.Low);
            }

            PulseEnable();
        }

        private void Write4Bits(byte value)
        {
            for (var i = 0; i < 4; i++)
            {
                _dataPorts[4 + i].Write(((value >> i) & 0x01) == 0x01 ? GpioPinValue.High : GpioPinValue.Low);
            }
            PulseEnable();
        }

        private void PulseEnable()
        {
            _enablePort.Write(GpioPinValue.Low);
            _enablePort.Write(GpioPinValue.High);  // enable pulse must be >450ns
            _enablePort.Write(GpioPinValue.Low); // commands need > 37us to settle
        }
    }
}