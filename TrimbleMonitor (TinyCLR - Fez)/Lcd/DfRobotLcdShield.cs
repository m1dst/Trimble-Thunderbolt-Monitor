using System.Threading;
using GHIElectronics.TinyCLR.Devices.Adc;
using GHIElectronics.TinyCLR.Pins;

namespace MicroLiquidCrystal
{
    public class DfRobotLcdShield : Lcd
    {

        readonly AdcChannel analog;

        public delegate void ButtonPressHandler(object sender, Buttons buttonPressed);
        public event ButtonPressHandler OnButtonPressed;

        const int KEY_UP = 600;
        const int KEY_DOWN = 1500;
        const int KEY_CMD1 = 2100;
        const int KEY_CMD2 = 100;
        const int KEY_CMD3 = 3000;
        const int KEY_NONE = 4000;

        private const int DEBOUNCE = 50;

        public DfRobotLcdShield(byte columns = 16, byte rows = 2)
            : base(new GpioLcdTransferProvider(
                FEZ.GpioPin.D8,
                FEZ.GpioPin.D9,
                FEZ.GpioPin.D4,
                FEZ.GpioPin.D5,
                FEZ.GpioPin.D6,
                FEZ.GpioPin.D7
            ))
        {

            var adcController = AdcController.GetDefault();
            analog = adcController.OpenChannel(FEZ.AdcChannel.A0);

            Begin(columns, rows);
            Clear();

            var buttonReader = new Thread(ReadButtons);
            buttonReader.Start();

        }

        public enum Buttons
        {
            Up,
            Down,
            Command1,
            Command2,
            Command3,
            None
        }

        private void ReadButtons()
        {
            var previousButton = Buttons.None;

            while (true)
            {
                var currentButton = ButtonState;

                if (currentButton != previousButton)
                {
                    OnButtonPressed?.Invoke(this, ButtonState);
                    previousButton = currentButton;
                }
                Thread.Sleep(DEBOUNCE);
            }
        }

        public Buttons ButtonState
        {
            get
            {
                var adcRaw = analog.ReadValue();
                //Debug.Print(adcRaw.ToString());
                if (adcRaw > KEY_NONE) return Buttons.None;
                if (adcRaw < KEY_CMD2) return Buttons.Command2;
                if (adcRaw < KEY_UP) return Buttons.Up;
                if (adcRaw < KEY_DOWN) return Buttons.Down;
                if (adcRaw < KEY_CMD1) return Buttons.Command1;
                if (adcRaw < KEY_CMD3) return Buttons.Command3;
                return Buttons.None;
            }
        }

        /// <summary>
        /// Writes an entire line of text to the LCD.
        /// </summary>
        /// <param name="line">The line required to write at.  Starts at 1.</param>
        /// <param name="text">The string to write.</param>
        /// <param name="align">Alignment of the string. EG: Left</param>
        public void WriteLine(int line, string text, TextAlign align = TextAlign.Left)
        {
            if (line < 0) { line = 0; }
            if (line > Lines - 1) { line = Lines - 1; }
            SetCursorPosition(0, line);
            WriteLine(text, align);
        }

        /// <summary>
        /// Writes an entire line of text to the LCD.
        /// </summary>
        /// <param name="text">The string to write.</param>
        /// <param name="align">Alignment of the string. EG: Left</param>
        public void WriteLine(string text, TextAlign align = TextAlign.Left)
        {

            if (text.Length > Columns)
            {
                // the string length was too long to fit on a line so trim it.
                text = text.Substring(0, Columns);
            }
            else if (text.Length < Columns)
            {
                // the string length was too short to fit on a line so pad it.
                var numberOfCharsToPad = Columns - text.Length;

                for (var i = 0; i < numberOfCharsToPad; i++)
                {
                    switch (align)
                    {
                        case TextAlign.Left:
                            text += " ";
                            break;
                        case TextAlign.Right:
                            text = " " + text;
                            break;
                        case TextAlign.Centre:
                            if (text.Length % 2 == 0)
                            { text = " " + text; }
                            else { text += " "; }
                            break;
                    }
                }
            }

            // write the string
            Write(text);

        }

    }

}