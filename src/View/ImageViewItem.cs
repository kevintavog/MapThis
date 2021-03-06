﻿using System;
using System.IO;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.ImageKit;
using System.Collections.Generic;
using Rangic.Utilities.Image;
using Rangic.Utilities.Geo;

namespace MapThis.View
{
	public class ImageViewItem : IKImageBrowserItem, IComparable
	{
		public string File { get; private set; }
		private NSUrl Url { get; set; }
        public bool IsVideo { get; private set; }

        public ImageViewItem(string file, bool isVideo)
		{
			File = file;
			Url = new NSUrl(File, false);

            IsVideo = isVideo;
		}

		public void UpdateKeywords()
		{
			_abbreviatedKeywords = null;
			_keywords = null;
            if (_imageDetails != null)
                _imageDetails.ReloadKeywords();
		}

        public void UpdateLocation(Location newLocation)
        {
            Location = newLocation;
            _gpsCoordinates = Location == null ? "" : Location.ToDms();
        }

		public override string ImageUID { get { return Url.Path; } }
        public override NSString ImageRepresentationType { get { return IsVideo ? IKImageBrowserItem.QTMoviePathRepresentationType : IKImageBrowserItem.PathRepresentationType; } }
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
                        _gpsCoordinates = Location.ToDms();
					}
					else
					{
						_gpsCoordinates = "";
					}
				}

				return _gpsCoordinates == "" ? null : _gpsCoordinates;
			}
		}

        public IEnumerable<string> KeywordsList
        {
            get
            {
                return ImageDetails.Keywords;
            }
        }

		private string _abbreviatedKeywords;
		private string AbbreviatedKeywords
		{
			get
			{
				if (_abbreviatedKeywords == null)
				{
                    if (ImageDetails.Keywords.Count() < 1)
					{
						_abbreviatedKeywords = "<>";
					}
					else
					{
                        _abbreviatedKeywords = ImageDetails.Keywords.First();
                        if (ImageDetails.Keywords.Count() > 1)
						{
                            _abbreviatedKeywords += " +" + (ImageDetails.Keywords.Count() - 1);
						}
					}
				}

				return _abbreviatedKeywords;
			}
		}

        private string KeywordCount { get { return ImageDetails.Keywords.Count().ToString(); } }
		private string _keywords;
		public string Keywords
		{
			get
			{
				if (_keywords == null)
				{
                    if (ImageDetails.Keywords.Count() < 1)
					{
						_keywords = "";
					}
					else
					{
                        _keywords = String.Join(", ", ImageDetails.Keywords);
					}
				}

				return _keywords == "" ? null : _keywords;
			}
		}


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
	}
}
