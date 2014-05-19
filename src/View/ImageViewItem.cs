﻿using System;
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
		public override string ImageTitle { get { return HasGps ? "GPS" : ""; } }
		public override string ImageSubtitle { get { return Path.GetFileNameWithoutExtension(File); } }

		public bool HasGps { get { return !String.IsNullOrEmpty(GpsCoordinates ); } }
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
						_gpsCoordinates = string.Format("{0}, {1}", Location.Latitude, Location.Longitude);
					}
					else
					{
						_gpsCoordinates = "";
					}
				}

				return _gpsCoordinates == "" ? null : _gpsCoordinates;
			}
		}
	}
}
