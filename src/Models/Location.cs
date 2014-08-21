using System;

namespace MapThis.Models
{
	public class Location
	{
		public double Latitude { get; private set; }
		public double Longitude { get; private set; }

		public Location(double latitude, double longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
		}

		public override string ToString()
		{
			return string.Format("[Location: Latitude={0}, Longitude={1}]", Latitude, Longitude);
		}

        public string ToDmsString()
        {
            char latNS = Latitude < 0 ? 'S' : 'N';
            char longEW = Longitude < 0 ? 'W' : 'E';
            return String.Format("{0} {1}, {2} {3}", ToDms(Latitude), latNS, ToDms(Longitude), longEW);
        }

        private string ToDms(double l)
        {
            if (l < 0)
            {
                l *= -1f;
            }
            var degrees = Math.Truncate(l);
            var minutes = (l - degrees) * 60f;
            var seconds = (minutes - (int) minutes) * 60;
            minutes = Math.Truncate(minutes);
            return String.Format("{0:00}° {1:00}' {2:00}\"", degrees, minutes, seconds);
        }

	}
}
