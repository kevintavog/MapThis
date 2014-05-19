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
	}
}
