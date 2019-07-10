using System;

namespace TrimbleMonitor.Thunderbolt
{
    public static class Helpers
    {

        public static FixMode ByteToFixMode(byte b)
        {
            if (b == 0)
                return FixMode.Auto;
            else
                return FixMode.Manual;
        }

        public static FixDimension ByteToFixDimension(byte b)
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

        public static FixPrecision FloatToFixPrecision(Single s)
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

        public static string FloatToFixPrecisionString(Single s)
        {
            if (s == 0)
                return "";
            else if (s == 1.0F)
                return "Ideal";
            else if (s <= 2.0F)
                return "Great";
            else if (s <= 5.0F)
                return "Good";
            else if (s <= 10.0F)
                return "Moderate";
            else if (s <= 20.0F)
                return "Fair";
            else
                return "Poor";
        }



    }
}
