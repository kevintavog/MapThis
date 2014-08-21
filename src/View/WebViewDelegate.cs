using System;
using MonoMac.Foundation;
using MapThis.Controllers;
using MonoMac.ObjCRuntime;
using NLog;
using MonoMac.WebKit;
using MapThis.Models;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		[Export ("logMessage:")]
		public void logMessage(NSString message)
		{
			logger.Info("js log: {0}", message);
		}

		[Export ("clicked:")]
		public void clicked(NSNumber lat, NSNumber lng)
		{
            var location = new Location(lat.DoubleValue, lng.DoubleValue);
            var message = string.Format("Looking up {0}...", location.ToDmsString()).Replace("\"", "\\\"");
            MapWebView.InvokeMapScript("setPopup([{0}, {1}], \"{2}\")", lat, lng, message);

			MapController.NameFromLocation(
                location,
				ReverseLocator.Filter.Minimal,
				(s) => 
			{
				BeginInvokeOnMainThread(delegate 
				{
					MapWebView.InvokeMapScript("setPopup([{0}, {1}], \"{2}\")", lat, lng, s); 
				});
			});
		}

        [Export ("updateMarker:")]
        public void updateMarker(NSString id, NSNumber lat, NSNumber lng)
        {
            logger.Info("updateMarker: {0} to {1}, {2}", id, lat, lng);
            var ms = GetMarker(id);
            if (ms == null)
            {
                logger.Warn("Unable to find marker: '{0}'", id);
            }
            else
            {
                UpdateMarker(ms, lat.DoubleValue, lng.DoubleValue);
            }
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

                case "updateMarker:":
                    return "updateMarker";
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
                case "updateMarker:":
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
