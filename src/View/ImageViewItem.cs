using System;
using System.IO;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.ImageKit;
using MapThis.Utilities;
using MapThis.Models;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace MapThis.View
{
	public class ImageViewItem : IKImageBrowserItem
	{
		public string File { get; private set; }
		private NSUrl Url { get; set; }

		private OrderedDictionary fileKeywords;

		public ImageViewItem(string file, OrderedDictionary keywords)
		{
			fileKeywords = keywords;
			File = file;
			Url = new NSUrl(File, false);
		}

		public override string ImageUID { get { return Url.Path; } }
		public override NSString ImageRepresentationType { get { return IKImageBrowserItem.PathRepresentationType; } }
		public override NSObject ImageRepresentation { get { return Url; } }
		public override string ImageTitle { get { return String.Format("{0} - {1}", HasGps ? "GPS" : "<>", AbbreviatedKeywords); } }
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

		private string _abbreviatedKeywords;
		private string AbbreviatedKeywords
		{
			get
			{
				if (_abbreviatedKeywords == null)
				{
					if (fileKeywords.Count < 1)
					{
						_abbreviatedKeywords = "<>";
					}
					else
					{
						_abbreviatedKeywords = fileKeywords.Keys.OfType<string>().First();
						if (fileKeywords.Count > 1)
						{
							_abbreviatedKeywords += ", +" + (fileKeywords.Count - 1);
						}
					}
				}

				return _abbreviatedKeywords;
			}
		}
		private string KeywordCount { get { return fileKeywords.Count.ToString(); } }
		private string _keywords;
		public string Keywords
		{
			get
			{
				if (_keywords == null)
				{
					if (fileKeywords.Count < 1)
					{
						_keywords = "";
					}
					else
					{
						_keywords = String.Join(", ", fileKeywords.Keys);
					}
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
