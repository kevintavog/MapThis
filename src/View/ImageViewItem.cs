using System;
using System.IO;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.ImageKit;
using MapThis.Utilities;
using MapThis.Models;
using System.Collections.Specialized;
using System.Collections.Generic;
using NLog;

namespace MapThis.View
{
	public class ImageViewItem : IKImageBrowserItem, IComparable
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

		public void UpdateKeywords(OrderedDictionary keywords)
		{
			fileKeywords = keywords;
			_abbreviatedKeywords = null;
			_keywords = null;
		}

        public void UpdateLocation(Location newLocation)
        {
            Location = newLocation;
            _gpsCoordinates = Location == null ? "" : Location.ToDmsString();
        }

		public override string ImageUID { get { return Url.Path; } }
		public override NSString ImageRepresentationType { get { return IKImageBrowserItem.PathRepresentationType; } }
		public override NSObject ImageRepresentation { get { return Url; } }
		public override string ImageTitle { get { return String.Format("{0} - {1}", Path.GetFileNameWithoutExtension(File), AbbreviatedKeywords); } }
		public override string ImageSubtitle { get { return HasGps ? GpsCoordinates : "<>"; } }

        public string MapTitle { get { return Path.GetFileNameWithoutExtension(File); } }
		public bool HasKeywords { get { return !String.IsNullOrEmpty(Keywords); } }
		public bool HasGps { get { return !String.IsNullOrEmpty(GpsCoordinates); } }
		public Location Location { get; private set; }

		public DateTime CreatedTimestamp { get { return ImageDetails.CreatedTime; } }

		private ImageDetails _imageDetails;
		private ImageDetails ImageDetails
		{
			get
			{
				if (_imageDetails == null)
				{
					_imageDetails = new ImageDetails(File);
				}
				return _imageDetails;
			}
		}

		private string _gpsCoordinates;
		public string GpsCoordinates
		{
			get
			{
				if (_gpsCoordinates == null)
				{
					Location = ImageDetails.Location;
					if (Location != null)
					{
                        _gpsCoordinates = Location.ToDmsString();
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
							_abbreviatedKeywords += " +" + (fileKeywords.Count - 1);
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
						_keywords = String.Join(", ", fileKeywords.Keys.OfType<string>());
					}
				}

				return _keywords == "" ? null : _keywords;
			}
		}


		#region IComparable implementation

		public int CompareTo(object obj)
		{
			ImageViewItem other = obj as ImageViewItem;
			if (other == null)
			{
				return -1;
			}

			int dateCompare = DateTime.Compare(CreatedTimestamp, other.CreatedTimestamp);
			if (dateCompare != 0)
			{
				return dateCompare;
			}

			return String.Compare(Path.GetFileName(File), Path.GetFileName(other.File));
		}

		#endregion
	}
}
