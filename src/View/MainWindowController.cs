﻿using System;
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


#region Constructors

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
//			directoryTree = new DirectoryTree("/Users/goatboy/Pictures/Master");
			directoryTree = new DirectoryTree("/Users/goatboy/Pictures/To Server/");
		}

#endregion

		public MapWebView MapWebView { get { return (MapWebView) webView; } }
		public new MainWindow Window { get { return (MainWindow) base.Window; } }


		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			InitializeImageFilters();
			MapWebView.MainFrame.LoadRequest(new NSUrlRequest(new NSUrl(NSBundle.MainBundle.PathForResource("map", "html"))));
			searchField.Delegate = new SearchTextFieldDelegate(this);
			keywordEntry.Delegate = new KeywordsTextFieldDelegate(this);
			Window.Delegate = new MainWindowDelegate(this);
			tabSplitView.Delegate = new SplitViewDelegate(this);


//			imageView.SetValueForKey(NSColor.ControlDarkShadow, IKImageBrowserView.BackgroundColorKey);

			MapWebView.CompleteDropAction = CompleteDrop;
		}

		internal void ScriptObjectAvailable()
		{
			// HACK: It seems setting this in AwakeFromNib is the right place, but it ends up with 0.5
			// rather than the default value (0.75). I don't know...
			imageView.ZoomValue = imageSizeSlider.FloatValue;
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

		public void CompleteDrop(double latitude, double longitude, IList<string> pathList)
		{
			MapWebView.InvokeMapScript("addMarker([{0}, {1}])", latitude, longitude);
			SetStatusText("Updating 1 of {0} file(s) to {1}, {2}", pathList.Count, latitude, longitude);

			Task.Run( () => GeoUpdater.UpdateFiles(
				pathList, 
				latitude, 
				longitude, 
				(s,i) => BeginInvokeOnMainThread( delegate 
				{ 
					SetStatusText("Updating {0} of {1} files to {2}, {3}", i, pathList.Count, latitude, longitude);
				}),
				() => BeginInvokeOnMainThread( delegate 
				{ 
					SetStatusText("Finished updating {0} files to {1}, {2}", pathList.Count, latitude, longitude);
				})));
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