using System;

namespace TrimbleMonitor.Thunderbolt
{
    public enum AngleUnits
    {
        Degrees,
        Radians
    }

    public enum AltitudeUnits
    {
        Feet,
        Meters
    }

    public class Position
    {
        private AltitudeUnits altitude_units;
        private AngleUnits angle_units;

        private Double latitude;
        private Double longitude;
        private Double altitude;

        public Position(AngleUnits angles, AltitudeUnits alts)
        {
            latitude = 0;
            longitude = 0;
            altitude = 0;
            altitude_units = alts;
            angle_units = angles;
        }

        public Position()
        {
            latitude = 0;
            longitude = 0;
            altitude = 0;
            altitude_units = AltitudeUnits.Feet;
            angle_units = AngleUnits.Degrees;
        }

        public override bool Equals(Object obj)
        {
            // If parameter is null return false.

            // If parameter cannot be cast to Position return false.
            var p = obj as Position;
            if ((object) p == null)
            {
                return false;
            }

            // Return true if all fields match
            return latitude == p.latitude && longitude == p.longitude && altitude == p.altitude;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal AltitudeUnits Altitude_Units
        {
            get => altitude_units;
            set => altitude_units = value;
        }

        public AngleUnits Angle_Units
        {
            get => angle_units;
            set => angle_units = value;
        }

        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        /// <summary>
        /// Returns 'S' or 'N' based on the value of the Latitude property
        /// </summary>
        public string LatitudeDirection => latitude < 0.0 ? " S" : " N";

        /// <summary>
        /// Returns 'E' or 'W' based on the value of the Longitude property
        /// </summary>
        public string LongitudeDirection => longitude < 0.0 ? " W" : " E";

        /// <summary>
        /// Gets or sets a latitude value. Set must always be in Radians, Get will return Degrees or Radians depending on the value of the Angle_Units property
        /// </summary>
        public Double Latitude
        {
            get => angle_units == AngleUnits.Degrees ? RadianToDegree(latitude) : latitude;
            set => latitude = value;
        }

        /// <summary>
        /// Gets or sets a longitude value. Set must always be in Radians, Get will return Degrees or Radians depending on the value of the Angle_Units property
        /// </summary>
        public Double Longitude
        {
            get => angle_units == AngleUnits.Degrees ? RadianToDegree(longitude) : longitude;
            set => longitude = value;
        }

        /// <summary>
        /// Gets or sets an altitude value. Set must always be in Meters, Get will return Feet or Meters depending on the value of the Altitude_Units property
        /// </summary>
        public Double Altitude
        {
            get => altitude_units == AltitudeUnits.Feet ? Round(altitude * 39.37 / 12.0, 1) : altitude;
            set => altitude = value;
        }

        private static float[] PowersOfTen = new float[]
        {
          1.0f,
          10.0f,
          100.0f,
          1000.0f,
          10000.0f,
          100000.0f,
          1000000.0f,
          // etc.
        };

        public static double Round(double value, int digits)
        {
            var power = PowersOfTen[digits];
            return power * (long)(value / power);
        }
    }
}
