using System;
using MonoMac.ImageKit;
using MonoMac.Foundation;
using System.IO;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
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
				mapController.ActivateLocation(item.Location);
				MapWebView.InvokeMapScript(
					"setPopup([{0}, {1}], \"{2}\")", 
					item.Location.Latitude, item.Location.Longitude, item.ImageSubtitle);
			}
		}

		[Export("imageBrowserSelectionDidChange:")]
		public void ImageBrowserSelectionDidChange(IKImageBrowserView view)
		{
			if (view.SelectionIndexes.Count == 1)
			{
				var item = ImageAtIndex(view.SelectionIndexes.FirstIndex);
				var gps = item.HasGps ? item.GpsCoordinates : "No GPS";
				SetStatusText("{0}    {1}", Path.GetFileName(item.File), gps);
			}
			else if (view.SelectionIndexes.Count > 1)
			{
				SetStatusText("{0} items selected", view.SelectionIndexes.Count);
			}
			else
			{
				SetStatusText("");
			}
		}

		private ImageViewItem ImageAtIndex(uint index)
		{
			return imageViewItems[(int) index];
		}
	}
}

