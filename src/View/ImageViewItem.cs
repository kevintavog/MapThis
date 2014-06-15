using System;
using System.IO;
using MonoMac.Foundation;
using MonoMac.ImageKit;
using MapThis.Utilities;
using MapThis.Models;

namespace MapThis.View
{
	public class ImageViewItem : IKImageBrowserItem
	{
		public string File { get; private set; }
		private NSUrl Url { get; set; }

		public ImageViewItem(string file)
		{
			File = file;
			Url = new NSUrl(File, false);
		}

		public override string ImageUID { get { return Url.Path; } }
		public override NSString ImageRepresentationType { get { return IKImageBrowserItem.PathRepresentationType; } }
		public override NSObject ImageRepresentation { get { return Url; } }
		public override string ImageTitle { get { return String.Format("{0}   {1}", HasGps ? "GPS" : "", HasKeywords ? KeywordCount.ToString() : ""); } }
		public override string ImageSubtitle { get { return Path.GetFileNameWithoutExtension(File); } }

		public bool HasKeywords { get { return !String.IsNullOrEmpty(Keywords); } }
		public bool HasGps { get { return !String.IsNullOrEmpty(GpsCoordinates); } }
		public Location Location { get; private set; }

		private string _gpsCoordinates;
		public string GpsCoordinates
		{
			get
			{
				if (_gpsCoordinates == null)
				{
					Location = ImageDetails.GetLocation(File);
					if (Location != null)
					{
						char latNS = Location.Latitude < 0 ? 'S' : 'N';
						char longEW = Location.Longitude < 0 ? 'W' : 'E';
						_gpsCoordinates = String.Format("{0} {1}, {2} {3}", ToDms(Location.Latitude), latNS, ToDms(Location.Longitude), longEW);
					}
					else
					{
						_gpsCoordinates = "";
					}
				}

				return _gpsCoordinates == "" ? null : _gpsCoordinates;
			}
		}


		private int KeywordCount { get; set; }
		private string _keywords;
		public string Keywords
		{
			get
			{
				if (_keywords == null)
				{
					_keywords = "";
				}

				return _keywords == "" ? null : _keywords;
			}
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
