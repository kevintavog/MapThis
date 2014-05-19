// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace MapThis.View
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSOutlineView directoryView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSPopUpButton imageFilterSelector { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider imageSizeSlider { get; set; }

		[Outlet]
		MonoMac.ImageKit.IKImageBrowserView imageView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField searchField { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView searchResults { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField statusLabel { get; set; }

		[Outlet]
		MonoMac.WebKit.WebView webView { get; set; }

		[Action ("fileType:")]
		partial void fileType (MonoMac.Foundation.NSObject sender);

		[Action ("imageSize:")]
		partial void imageSize (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (directoryView != null) {
				directoryView.Dispose ();
				directoryView = null;
			}

			if (imageFilterSelector != null) {
				imageFilterSelector.Dispose ();
				imageFilterSelector = null;
			}

			if (imageSizeSlider != null) {
				imageSizeSlider.Dispose ();
				imageSizeSlider = null;
			}

			if (imageView != null) {
				imageView.Dispose ();
				imageView = null;
			}

			if (searchField != null) {
				searchField.Dispose ();
				searchField = null;
			}

			if (searchResults != null) {
				searchResults.Dispose ();
				searchResults = null;
			}

			if (statusLabel != null) {
				statusLabel.Dispose ();
				statusLabel = null;
			}

			if (webView != null) {
				webView.Dispose ();
				webView = null;
			}
		}
	}

	[Register ("MainWindow")]
	partial class MainWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
