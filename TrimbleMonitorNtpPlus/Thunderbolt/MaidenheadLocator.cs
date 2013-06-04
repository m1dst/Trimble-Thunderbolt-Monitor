using System;

namespace TrimbleMonitorNtpPlus.Thunderbolt
{

    /// <summary>
    /// Class providing static methods for calculating with Maidenhead locators, especially
    /// distance and bearing. Based on the Perl script by Dirk Koopman G1TLH from 2002-11-07,
    /// found at http://www.koders.com/perl/fidDAB6FD208AC4F5C0306CA344485FD0899BD2F328.aspx
    /// </summary>
    public class MaidenheadLocator
    {
        /// <summary>
        /// Simple structure to store a position in latitude and longitude
        /// </summary>
        public struct LatLong : IComparable
        {
            /// <summary>
            /// Latitude, -90 to +90 (N/S direction)
            /// </summary>
            public double Lat;
            /// <summary>
            /// Longitude, -180 to +180 (W/E direction)
            /// </summary>
            public double Long;

            public override string ToString()
            {
                return Long.ToString("#.###") + (Long >= 0 ? "N" : "S") + " " + Lat.ToString("#.###") + (Lat >= 0 ? "E" : "W");
            }

            public int CompareTo(object to)
            {
                if (to is LatLong)
                {
                    if (Lat == ((LatLong)to).Lat && Long == ((LatLong)to).Long) return 0;
                    return -1;
                }
                return -1;
            }
        }

        /// <summary>
        /// Convert latitude and longitude in degrees to a locator
        /// </summary>
        /// <param name="ll">LatLong structure to convert</param>
        /// <returns>Locator string</returns>
        public static string LatLongToLocator(LatLong ll)
        {
            return LatLongToLocator(ll.Lat, ll.Long, 0);
        }

        /// <summary>
        /// Convert latitude and longitude in degrees to a locator
        /// </summary>
        /// <param name="ll">LatLong structure to convert</param>
        /// <param name="Ext">Extra precision (0, 1, 2)</param>
        /// <returns>Locator string</returns>
        public static string LatLongToLocator(LatLong ll, int Ext)
        {
            return LatLongToLocator(ll.Lat, ll.Long, Ext);
        }

        /// <summary>
        /// Convert latitude and longitude in degrees to a locator
        /// </summary>
        /// <param name="Lat">Latitude to convert</param>
        /// <param name="Long">Longitude to convert</param>
        /// <returns>Locator string</returns>
        public static string LatLongToLocator(double Lat, double Long)
        {
            return LatLongToLocator(Lat, Long, 0);
        }

        /// <summary>
        /// Convert latitude and longitude in degrees to a locator
        /// </summary>
        /// <param name="Lat">Latitude to convert</param>
        /// <param name="Long">Longitude to convert</param>
        /// <param name="Ext">Extra precision (0, 1, 2)</param>
        /// <returns>Locator string</returns>
        public static string LatLongToLocator(double Lat, double Long, int Ext)
        {
            string locator = "";

            Lat += 90;
            Long += 180;

            locator += (char)('A' + System.Math.Floor(Long / 20));
            locator += (char)('A' + System.Math.Floor(Lat / 10));
            Long = IEEERemainder(Long, 20);
            if (Long < 0) Long += 20;
            Lat = IEEERemainder(Lat, 10);
            if (Lat < 0) Lat += 10;

            locator += (char)('0' + System.Math.Floor(Long / 2));
            locator += (char)('0' + System.Math.Floor(Lat / 1));
            Long = IEEERemainder(Long, 2);
            if (Long < 0) Long += 2;
            Lat = IEEERemainder(Lat, 1);
            if (Lat < 0) Lat += 1;

            locator += (char)('A' + System.Math.Floor(Long * 12));
            locator += (char)('A' + System.Math.Floor(Lat * 24));
            Long = IEEERemainder(Long, (double)1 / 12);
            if (Long < 0) Long += (double)1 / 12;
            Lat = IEEERemainder(Lat, (double)1 / 24);
            if (Lat < 0) Lat += (double)1 / 24;

            if (Ext >= 1)
            {
                locator += (char)('0' + System.Math.Floor(Long * 120));
                locator += (char)('0' + System.Math.Floor(Lat * 240));
                Long = IEEERemainder(Long, (double)1 / 120);
                if (Long < 0) Long += (double)1 / 120;
                Lat = IEEERemainder(Lat, (double)1 / 240);
                if (Lat < 0) Lat += (double)1 / 240;
            }

            if (Ext >= 2)
            {
                locator += (char)('A' + System.Math.Floor(Long * 120 * 24));
                locator += (char)('A' + System.Math.Floor(Lat * 240 * 24));
                Long = IEEERemainder(Long, (double)1 / 120 / 24);
                if (Long < 0) Long += (double)1 / 120 / 24;
                Lat = IEEERemainder(Lat, (double)1 / 240 / 24);
                if (Lat < 0) Lat += (double)1 / 240 / 24;
            }

            return locator;

        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static double RadToDeg(double rad)
        {
            return rad / System.Math.PI * 180;
        }

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegToRad(double deg)
        {
            return deg / 180 * System.Math.PI;
        }

        public static double IEEERemainder(double x, double y)
        {
            return x - (y * System.Math.Round(x / y));
        }


    }
}