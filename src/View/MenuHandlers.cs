using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MapThis.Models;
using System.Collections.Generic;
using MapThis.Controllers;
using System.Threading.Tasks;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		[Export("showKeywords:")]
		public void ShowKeywords(NSObject sender)
		{
			tabView.Select(keywordsTab);
			keywordEntry.SelectText(sender);
		}

		[Export("showMap:")]
		public void ShowMap(NSObject sender)
		{
			tabView.Select(mapTab);
			searchField.SelectText(sender);
		}

        [Export("clearLocations:")]
        public void ClearLocations(NSObject sender)
        {
            var selectedFiles = new List<string>();
            if (imageView.SelectionIndexes.Count > 0)
            {
                foreach (var index in imageView.SelectionIndexes.ToArray())
                {
                    selectedFiles.Add(ImageAtIndex(index).File);
                }
            }

            if (selectedFiles.Count < 1)
                return;

            SetStatusText("Clearing location from {0} file(s)", selectedFiles.Count);

            Task.Run( () => GeoUpdater.ClearLocation(
                selectedFiles,
                () => BeginInvokeOnMainThread( delegate 
                {
                    foreach (var f in selectedFiles)
                    {
                        var imageItem = ImageItemFromPath(f);
                        if (imageItem != null)
                        {
                            imageItem.UpdateLocation(null);
                        }
                    }

                    SetStatusText("Finished clearing location from {0} files", selectedFiles.Count);
                    imageView.ReloadData();
                })));
        }

		[Export("showImages:")]
		public void ShowImages(NSObject sender)
		{
			Window.MakeFirstResponder(imageView);
		}

        [Export("showImagesOnMap:")]
        public void ShowImagesOnMap(NSObject sender)
        {
            // All images in current view, whether selcted or not
            logger.Info("Show images on map");
            ClearAllMarkers();
            tabView.Select(mapTab);
            double minLat = 90.0;
            double maxLat = -90.0;
            double minLng = 180.0;
            double maxLng = -180.0;
            bool setGps = false;

            foreach (var item in imageViewItems)
            {
                if (item.HasGps)
                {
                    setGps = true;
                    minLat = Math.Min(minLat, item.Location.Latitude);
                    maxLat = Math.Max(maxLat, item.Location.Latitude);

                    minLng = Math.Min(minLng, item.Location.Longitude);
                    maxLng = Math.Max(maxLng, item.Location.Longitude);
                }
            }

            if (!setGps)
            {
                return;
            }

            MapWebView.InvokeMapScript("fitToBounds([[{0}, {1}],[{2}, {3}]])", minLat, minLng, maxLat, maxLng);

            foreach (var item in imageViewItems)
            {
                if (item.HasGps)
                {
                    var markerSet = CreateMarkerSet(item.File);
                    var tooltip = string.Format("{0}\\n{1}", item.MapTitle, item.Keywords);
                    MapWebView.InvokeMapScript(
                        "addMarker({0}, [{1}, {2}], \"{3}\")", 
                        markerSet.Id, 
                        item.Location.Latitude, 
                        item.Location.Longitude, 
                        tooltip);
                }
            }
        }

		[Export("openFolder:")]
		public void OpenFolder(NSObject sender)
		{
			var openPanel = new NSOpenPanel
			{
				ReleasedWhenClosed = true,
				Prompt = "Select",
				CanChooseDirectories = true,
				CanChooseFiles = false
			};

			var result = (NsButtonId)openPanel.RunModal();
			if (result != NsButtonId.OK)
			{
				return;
			}

			OpenFolderDirectly(openPanel.Url.Path);
		}

		[Export("viewFile:")]
		public void ViewFile(NSObject sender)
		{
			if (imageView.SelectionIndexes.Count >= 1)
			{
				var item = ImageAtIndex(imageView.SelectionIndexes.FirstIndex);
				if (false == NSWorkspace.SharedWorkspace.OpenFile(item.File))
				{
					logger.Error("Unable to open '{0}'", item.File);
				}
			}
		}

		[Export("selectAllFiles:")]
		public void SelectAllFiles(NSObject sender)
		{
			if (Window.FirstResponder == directoryView || Window.FirstResponder == imageView)
			{
				if (imageViewItems.Count > 0)
				{
					imageView.SelectItemsAt(NSIndexSet.FromNSRange(new NSRange(0, imageViewItems.Count)), false);
				}
			}
			else
			if (Window.FirstResponder == keywordEntry.CurrentEditor)
			{
				keywordEntry.SelectText(sender);
			}
		}

        [Export("selectFilesWithoutKeywords:")]
        public void SelectFilesWithoutKeywords(NSObject sender)
        {
            var items = SelectItems( (item) => !item.HasKeywords );
            if (items.Length > 0)
            {
                imageView.ScrollIndexToVisible((int) items[0]);
            }
        }

        [Export("selectFilesWithoutLocations:")]
        public void SelectFilesWithoutLocations(NSObject sender)
        {
            logger.Info("Select files without locations");
            var items = SelectItems( (item) => !item.HasGps );
            if (items.Length > 0)
            {
                imageView.ScrollIndexToVisible((int) items[0]);
            }
        }

        private uint[] SelectItems(Func<ImageViewItem,bool> isSelected)
        {
            var set = new NSMutableIndexSet();
            imageView.SelectItemsAt(set, false);

            for (int idx = 0; idx < imageViewItems.Count; ++idx)
            {
                var item = imageViewItems[idx];
                if (isSelected(item))
                    set.Add((uint) idx);
            }

            imageView.SelectItemsAt(set, false);

            return set.Count == 0 ? new uint[0] : set.ToArray();
        }

		public bool OpenFolderDirectly(string path)
		{
			directoryTree = new DirectoryTree(path);
			directoryView.ReloadData();
			directoryView.SelectRow(0, false);

			Preferences.Instance.LastOpenedFolder = path;
			Preferences.Instance.Save();

			// That's gross - Mono exposes SharedDocumentController as NSObject rather than NSDocumentcontroller
			(NSDocumentController.SharedDocumentController as NSDocumentController).NoteNewRecentDocumentURL(new NSUrl(path, true));
			return true;
		}

	}

	public enum NsButtonId
	{
		Cancel = 0,
		OK = 1
	}
}
