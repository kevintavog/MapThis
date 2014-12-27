using System;
using Rangic.Utilities.Geo;

namespace MapThis.Models
{
	public class SearchArea
	{
		public Location Location { get; private set; }
		public Area Area { get; private set; }

		public string PlaceName { get { return Area.PlaceName; } }

		public double Top { get { return Area.UpperLeft.Latitude; } }
		public double Left { get { return Area.UpperLeft.Longitude; } }
		public double Bottom { get { return Area.LowerRight.Latitude; } }
		public double Right { get { return Area.LowerRight.Longitude; } }

		public SearchArea(Area area, Location location)
		{
			Area = area;
			Location = location;
		}

		public override string ToString()
		{
			return string.Format("[SearchArea: PlaceName={2}, Location={0}, Top={3}, Left={4}, Bottom={5}, Right={6}]", Location, Area, PlaceName, Top, Left, Bottom, Right);
		}
	}
}
