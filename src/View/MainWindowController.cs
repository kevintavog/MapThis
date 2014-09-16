using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.WebKit;
using NLog;
using MonoMac.ObjCRuntime;
using MapThis.Controllers;
using MapThis.Models;
using MonoMac.ImageKit;
using System.Threading.Tasks;
using System.IO;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		static private readonly Logger logger = LogManager.GetCurrentClassLogger();
		private LocationSearch locationSearch;
		private IList<SearchArea> searchData = new List<SearchArea>();
		private MapController mapController;
		private DirectoryTree directoryTree;
		private List<ImageViewItem> imageViewItems = new List<ImageViewItem>();
        private IDictionary<string,MarkerSet> allMarkers = new Dictionary<string,MarkerSet>();


		public MainWindowController(IntPtr handle) : base(handle)
		{
			Initialize();
		}

		[Export("initWithCoder:")]
		public MainWindowController(NSCoder coder) : base(coder)
		{
			Initialize();
		}

		public MainWindowController() : base("MainWindow")
		{
			Initialize();
		}

		void Initialize()
		{
			locationSearch = new LocationSearch( (r) => BeginInvokeOnMainThread(delegate { SearchComplete(r); } ));
			mapController = new MapController( (s,a) => { MapWebView.InvokeMapScript(s,a); } );
		}


		public MapWebView MapWebView { get { return (MapWebView) webView; } }
		public new MainWindow Window { get { return (MainWindow) base.Window; } }


		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			InitializeImageFilters();
			MapWebView.MainFrame.LoadRequest(new NSUrlRequest(new NSUrl(NSBundle.MainBundle.PathForResource("map", "html"))));
			searchField.Delegate = new SearchTextFieldDelegate(this);
			Window.Delegate = new MainWindowDelegate(this);
			tabSplitView.Delegate = new SplitViewDelegate(this);

            imageView.SetValueForKey(NSColor.DarkGray, IKImageBrowserView.BackgroundColorKey);
            var oldAttrs = imageView.ValueForKey(IKImageBrowserView.CellsTitleAttributesKey);
            var newAttrs = oldAttrs.MutableCopy();
            newAttrs.SetValueForKey(NSColor.White, NSAttributedString.ForegroundColorAttributeName);
            imageView.SetValueForKey(newAttrs, IKImageBrowserView.CellsTitleAttributesKey);
            imageView.SetValueForKey(newAttrs, IKImageBrowserView.CellsSubtitleAttributesKey);


			keywordEntry.Changed += delegate(object sender, EventArgs args) { KeywordsTextChanged((NSNotification) sender); };
			keywordEntry.DoCommandBySelector += KeywordsCommandSelector;
			keywordEntry.GetCompletions += KeywordsGetCompletions;
			keywordEntry.EditingEnded += delegate(object sender, EventArgs e) { ApplyKeyword(); };

			imageFilterSelector.SelectItem(Preferences.Instance.ImageFilterIndex);
			imageTypes = imageFilters[Preferences.Instance.ImageFilterIndex].Types;

			if (Preferences.Instance.LastOpenedFolder != null)
			{
				directoryTree = new DirectoryTree(Preferences.Instance.LastOpenedFolder);
			}
			else
			{
				var urlList = new NSFileManager().GetUrls(NSSearchPathDirectory.PicturesDirectory, NSSearchPathDomain.User);
				directoryTree = new DirectoryTree(urlList[0].Path);
			}

			var selectedPath = Preferences.Instance.LastSelectedFolder;
			if (!String.IsNullOrEmpty(selectedPath))
			{
				int bestRow = -1;
				for (int row = 0; row < directoryView.RowCount; ++row)
				{
					var dt = directoryView.ItemAtRow(row) as DirectoryTree;
					if (dt.Path == selectedPath)
					{
						bestRow = row;
						break;
					}

					if (selectedPath.StartsWith(dt.Path))
					{
						bestRow = row;
					}
				}

				if (bestRow >= 0)
				{
					directoryView.SelectRow(bestRow, false);
				}
			}

			MapWebView.CompleteDropAction = CompleteDrop;
		}

		internal void ScriptObjectAvailable()
		{
			// HACK: It seems setting this in AwakeFromNib is the right place, but it ends up with 0.5
			// rather than the default value (0.75). I don't know...
			UpdateImageViewZoom();
			MapWebView.WindowScriptObject.SetValueForKey(this, new NSString("MapThis"));
		}

		internal void FinishedLoading()
		{
			var lat = 47.6220;
			var lon = -122.335;
			MapWebView.InvokeMapScript("setCenter([{0}, {1}], 12)", lat, lon);
			SetTableColumns();
		}

		private void SetStatusText(string message, params object[] args)
		{
			statusLabel.StringValue = String.Format(message, args);
		}

        private MarkerSet CreateMarkerSet(string filePath)
        {
            var list = new List<string>();
            list.Add(filePath);
            return CreateMarkerSet(list);
        }

        private MarkerSet CreateMarkerSet(IList<string> pathList)
        {
            // Ensure files aren't in multiple marker sets - that's confusing.
            HashSet<MarkerSet> removeSet = new HashSet<MarkerSet>();
            foreach (var ms in allMarkers.Values)
            {
                foreach (var path in pathList)
                {
                    if (ms.Files.Contains(path))
                    {
                        removeSet.Add(ms);
                        break;
                    }
                }
            }

            foreach (var ms in removeSet)
            {
                MapWebView.InvokeMapScript("removeMarker({0})", ms.Id);
            }

            var markerSet = new MarkerSet { Files = pathList };
            allMarkers.Add(markerSet.Id, markerSet);
            return markerSet;
        }

        private void ClearAllMarkers()
        {
            MapWebView.InvokeMapScript("removeAllMarkers()");
            allMarkers.Clear();
        }

		public void CompleteDrop(double latitude, double longitude, IList<string> pathList)
		{
            var markerSet = CreateMarkerSet(pathList);
            MapWebView.InvokeMapScript("addMarker({0}, [{1}, {2}], \"{3}\")", markerSet.Id, latitude, longitude, markerSet.Title);

            // Exclude any files with existing geolocations - don't overwrite, unless a single file is selected
            var updateList = new List<string>();
            var skipList = new List<string>();
            foreach (var f in pathList)
            {
                var ii = ImageItemFromPath(f);
                if (ii != null)
                {
                    if (ii.HasGps && pathList.Count > 1)
                    {
                        skipList.Add(Path.GetFileName(f));
                        logger.Info("Not setting location on {0}", f);
                    }
                    else
                        updateList.Add(f);
                }
            }

            UpdateFileLocations(updateList, latitude, longitude, skipList.Count < 1);

            if (skipList.Count > 0)
            {
                BeginInvokeOnMainThread(delegate 
                {
                    SetStatusText("Some files were not updated due to existing location information: {0}", String.Join(", ", skipList));
                });
            }
		}

        private void UpdateFileLocations(IList<string> pathList, double latitude, double longitude, bool updateStatusText = true)
        {
            if (pathList.Count < 1)
                return;

            var location = new Location(latitude, longitude);
            if (updateStatusText)
                SetStatusText("Updating {0} file(s) to {1}", pathList.Count, location.ToDmsString());

            Task.Run( () => GeoUpdater.UpdateFiles(
                pathList, 
                latitude, 
                longitude,
                () => BeginInvokeOnMainThread( delegate 
                { 
                    foreach (var f in pathList)
                    {
                        var imageItem = ImageItemFromPath(f);
                        if (imageItem != null)
                        {
                            imageItem.UpdateLocation(location);
                        }
                    }

                    if (updateStatusText)
                    {
                        SetStatusText("Finished updating {0} files to {1}", pathList.Count, location.ToDmsString());
                    }
                    imageView.ReloadData();
                })));
        }

        private MarkerSet GetMarker(string markerId)
        {
            MarkerSet ms;
            if (allMarkers.TryGetValue(markerId, out ms))
            {
                return ms;
            }

            return null;
        }

        private void UpdateMarker(MarkerSet markerSet, double latitude, double longitude)
        {
            UpdateFileLocations(markerSet.Files, latitude, longitude);
        }

		private void ImageFilesSelected(IList<string> selectedFiles)
		{
			KeywordFilesChanged(selectedFiles);
		}

		private void WindowDidResize()
		{
			SetTableColumns();
		}

		private void SplitViewDidResize(NSSplitView splitView)
		{
			SetTableColumns();
		}

		private class SplitViewDelegate : NSSplitViewDelegate
		{
			MainWindowController controller;

			public SplitViewDelegate(MainWindowController controller)
			{
				this.controller = controller;
			}

			public override void DidResizeSubviews(NSNotification notification)
			{
				controller.SplitViewDidResize(notification.Object as NSSplitView);
			}
		}

		private class MainWindowDelegate : NSWindowDelegate
		{
			MainWindowController controller;

			public MainWindowDelegate(MainWindowController controller)
			{
				this.controller = controller;
			}

			public override void DidResize(NSNotification notification)
			{
				controller.WindowDidResize();
			}
		}
	}
}