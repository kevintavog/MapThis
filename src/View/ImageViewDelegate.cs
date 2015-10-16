using System;
using MonoMac.ImageKit;
using MonoMac.Foundation;
using System.IO;
using System.Collections.Generic;
using MonoMac.AppKit;
using System.Drawing;
using System.Linq;

namespace MapThis.View
{
	public partial class MainWindowController
	{
		private void ImageFilesUpdated(IEnumerable<string> filenames)
		{
			foreach (var imageItem in imageViewItems)
			{
				foreach (var f in filenames)
				{
					if (f == imageItem.File)
					{
						imageItem.UpdateKeywords();
						break;
					}
				}
			}
		}

		[Export("numberOfItemsInImageBrowser:")]
		public int ImageKitNumberOfItems(IKImageBrowserView view)
		{
			return imageViewItems.Count;
		}

		[Export("imageBrowser:itemAtIndex:")]
		public NSObject ImageKitItemAtIndex(IKImageBrowserView view, uint index)
		{
			return ImageAtIndex(index);
		}

		[Export("imageBrowser:cellWasDoubleClickedAtIndex:")]
		public void ImageBrowserCellWasDoubleClickedAtIndex(IKImageBrowserView view, uint index)
		{
			var item = ImageAtIndex(index);
			if (item.HasGps)
			{
                var markerSet = CreateMarkerSet(item.File);

                mapController.ActivateLocation(item.Location);
                var tooltip = string.Format("{0}\\n{1}", item.MapTitle, item.Keywords);
                MapWebView.InvokeMapScript(
                    "addMarker('{0}', [{1}, {2}], \"{3}\")", 
                    markerSet.Id, 
                    item.Location.Latitude, 
                    item.Location.Longitude, 
                    tooltip);
			}
		}

		[Export("imageBrowserSelectionDidChange:")]
		public void ImageBrowserSelectionDidChange(IKImageBrowserView view)
		{
			if (view.SelectionIndexes.Count == 1)
			{
				var item = ImageAtIndex(view.SelectionIndexes.FirstIndex);
				var gps = item.HasGps ? item.GpsCoordinates : "No GPS";
				SetStatusText("{0}    {1}     {2}", Path.GetFileName(item.File), gps, item.Keywords);
			}
			else if (view.SelectionIndexes.Count > 1)
			{
				SetStatusText("{0} items selected", view.SelectionIndexes.Count);
			}
			else
			{
                SetFolderStatus();
			}

			var selectedFiles = new List<string>();
			if (view.SelectionIndexes.Count > 0)
			{
				foreach (var index in view.SelectionIndexes.ToArray())
				{
					selectedFiles.Add(ImageAtIndex(index).File);
				}
			}
			ImageFilesSelected(selectedFiles);
		}

		[Export("view:stringForToolTip:point:userData:")]
		public NSString ViewStringForToolTip(NSView view, NSObject tooltipTag, PointF point, IntPtr data)
		{
			int index = data.ToInt32();
			if (index >= 0 && index < imageViewItems.Count)
			{
				return (NSString) imageViewItems[index].Keywords;
			}

			logger.Info("Nothing for {0}", index);
			return null;
		}

        private void SetFolderStatus()
        {
            var keywords = new HashSet<string>();
            var countGps = 0;
            var countKeywords = 0;
            foreach (var item in imageViewItems)
            {
                if (item.HasGps)
                    ++countGps;
                if (item.HasKeywords)
                {
                    ++countKeywords;
                    foreach (var k in item.KeywordsList)
                        keywords.Add(k);
                }
            }

            SetStatusText("{0} files; {1} with GPS; {2} with keywords; {3}", 
                imageViewItems.Count, 
                countGps, 
                countKeywords,
                String.Join(", ", keywords.OrderBy(s => s)));
        }

        private ImageViewItem ImageAtIndex(uint index)
        {
            return imageViewItems[(int) index];
        }

        private ImageViewItem ImageItemFromPath(string path)
		{
            foreach (var item in imageViewItems)
            {
                if (item.File == path)
                {
                    return item;
                }
            }
            return null;
		}

		private void UpdateImageViewZoom()
		{
			imageView.ZoomValue = imageSizeSlider.FloatValue;
			ConfigureImageViewTooltips();
		}

		private void ConfigureImageViewTooltips()
		{
			imageView.RemoveAllToolTips();
			for (int idx = 0; idx < imageViewItems.Count; ++idx)
			{
				var rect = imageView.GetItemFrame(idx);
				imageView.AddToolTip(rect, this, new IntPtr(idx));
			}
		}
	}
}
