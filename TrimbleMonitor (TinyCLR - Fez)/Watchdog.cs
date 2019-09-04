using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TrimbleMonitor.TinyCLR.Fez
{
    public static class WatchDog
    {
        public static bool LastReboot
        {
            get
            {
                var rccAddr = new IntPtr(0x40023800);
                int rccCsrValue = Marshal.ReadInt32(rccAddr, 0x74);
                return IsIwdgRstf(rccCsrValue);
            }
        }

        public static void Start(TimeSpan period)
        {
            ResetLastReboot();
            SetTimings(period);
            WriteIwdgKr(0xCCCC);
        }

        public static void Reset()
        {
            WriteIwdgKr(0xAAAA);
        }

        private static void ResetLastReboot()
        {
            var rccAddr = new IntPtr(0x40023800);
            int rccCsrValue = Marshal.ReadInt32(rccAddr, 0x74);

            if (IsIwdgRstf(rccCsrValue))
            {
                const int rmvfMask = 0x01000000;
                rccCsrValue = rccCsrValue | rmvfMask;
                Marshal.WriteInt32(rccAddr, 0x74, rccCsrValue);
            }
        }

        private static void WriteIwdgKr(int value)
        {
            Marshal.WriteInt32(new IntPtr(0x40003000), value);
        }

        private static bool IsIwdgRstf(int rccCsrValue)
        {
            const int iwdgRstfMask = 0x20000000;
            return (rccCsrValue & iwdgRstfMask) > 0;
        }

        private static void SetTimings(TimeSpan period)
        {
            const int kHzLsi = 32000;

            long usPeriod = ((period.Ticks * 1000) / TimeSpan.TicksPerMillisecond);
            int[] dividers = { 4, 8, 16, 32, 64, 128, 256 };
            for (int i = 0; i < dividers.Length; i++)
            {
                int usMin = (dividers[i] * 1000 * 1000) / kHzLsi;
                if (usPeriod >= usMin)
                {
                    int counter = (int)(usPeriod / usMin - 1);
                    if (counter < 0 || counter > 0xFFF)
                        continue;

                    SetIwdgPrAndRlr(i, counter);
                    return;
                }
            }

            throw new InvalidOperationException("Invalid period (0.125..32768 ms).");
        }

        private static void SetIwdgPrAndRlr(int prValue, int rlrValue)
        {
            var iwdgKrAddr = new IntPtr(0x40003000);
            Marshal.WriteInt32(iwdgKrAddr, 0x5555);
            Marshal.WriteInt32(iwdgKrAddr, 0x04, prValue);
            Marshal.WriteInt32(iwdgKrAddr, 0x08, rlrValue);
        }
    }
}
