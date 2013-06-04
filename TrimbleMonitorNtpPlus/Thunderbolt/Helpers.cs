using System;
using Microsoft.SPOT;

namespace TrimbleMonitorNtpPlus.Thunderbolt
{
    public static class Helpers
    {

        static public FixMode ByteToFixMode(byte b)
        {
            if (b == 0)
                return FixMode.Auto;
            else
                return FixMode.Manual;
        }

        static public FixDimension ByteToFixDimension(byte b)
        {
            if (b == 1)
                return FixDimension.Clock_1D;
            else if (b == 3)
                return FixDimension.Position_2D;
            else if (b == 4)
                return FixDimension.Position_3D;
            else if (b == 5)
                return FixDimension.OverDetermined;
            else
                return FixDimension.None;
        }

        static public FixPrecision FloatToFixPrecision(Single s)
        {
            if (s == 1.0F)
                return FixPrecision.Ideal;
            else if (s <= 2.0F)
                return FixPrecision.Excellent;
            else if (s <= 5.0F)
                return FixPrecision.Good;
            else if (s <= 10.0F)
                return FixPrecision.Moderate;
            else if (s <= 20.0F)
                return FixPrecision.Fair;
            else
                return FixPrecision.Poor;
        }



    }
}
