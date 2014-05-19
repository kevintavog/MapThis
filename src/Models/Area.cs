using System;

namespace MapThis.Models
{
	public class Area
	{
		public Location UpperLeft { get; private set; }
		public Location LowerRight { get; private set; }
		public String PlaceName { get; private set; }

		public Area(Location upperLeft, Location lowerRight, string placeName)
		{
			UpperLeft = upperLeft;
			LowerRight = lowerRight;
			PlaceName = placeName;
		}

		public override string ToString()
		{
			return string.Format("[Area: UpperLeft={0}, LowerRight={1}, PlaceName={2}]", UpperLeft, LowerRight, PlaceName);
		}
	}
}
