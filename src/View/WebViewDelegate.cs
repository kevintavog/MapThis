using System;
using MonoMac.Foundation;
using MapThis.Controllers;
using MonoMac.ObjCRuntime;
using NLog;
using MonoMac.WebKit;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		[Export ("message:")]
		public string message()
		{
			return "Hello world";
		}

		[Export ("logMessage:")]
		public void logMessage(NSString message)
		{
			logger.Info("js log: {0}", message);
		}

		[Export ("clicked:")]
		public void clicked(NSNumber lat, NSNumber lng)
		{
			MapWebView.InvokeMapScript("addMarker([{0}, {1}])", lat, lng);
			//			InvokeMapScript("setPopup([{0}, {1}], \"{2}\")", lat, lng, "Looking up name...");

			MapController.NameFromLocation(
				lat.DoubleValue,
				lng.DoubleValue,
				ReverseLocator.Filter.Minimal,
				(s) => 
			{
				BeginInvokeOnMainThread(delegate 
				{
					MapWebView.InvokeMapScript("setPopup([{0}, {1}], \"{2}\")", lat, lng, s); 
				});
			});
		}

		[Export ("webScriptNameForSelector:")]
		static string webScriptNameForSelector(Selector sel)
		{
			switch (sel.Name)
			{
				case "clicked:":
					return "clicked";

				case "logMessage:":
					return "logMessage";

				case "message:":
					return "message";
			}

			return "";
		}

		[Export ("isSelectorExcludedFromWebScript:")]
		static bool isSelectorExcludedFromWebScript(Selector sel)
		{
			switch (sel.Name)
			{
				case "clicked:":
				case "logMessage:":
				case "message:":
					return false;
			}

			return true;
		}

		[Export("webView:windowScriptObjectAvailable:")]
		public void WindowScriptObjectAvailable(WebView webView, WebScriptObject windowScriptObject)
		{
			ScriptObjectAvailable();
		}

		[Export("webView:didFinishLoadForFrame:")]
		public void FinishedLoad(WebView sender, WebFrame forFrame)
		{
			FinishedLoading();
		}

	}
}

