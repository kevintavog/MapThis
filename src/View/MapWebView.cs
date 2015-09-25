using System;
using MonoMac.WebKit;
using MonoMac.Foundation;
using NLog;
using MonoMac.AppKit;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace MapThis
{
	[MonoMac.Foundation.Register("MapWebView")]
	public class MapWebView : WebView
	{
		static private readonly Logger logger = LogManager.GetCurrentClassLogger();

		public MapWebView(IntPtr handle) : base(handle)
		{
			Initialize();
		}

		[Export ("initWithCoder:")]
		public MapWebView(NSCoder coder) : base(coder)
		{
			Initialize();
		}

		void Initialize()
		{
		}

		public Action<double, double, IList<string>> CompleteDropAction { get; set; }

		internal NSObject InvokeMapScript(string script, params object[] args)
		{
			var s = String.Format(script, args);
			logger.Info("Script: \"{0}\"", s);
			return WindowScriptObject.EvaluateWebScript(s);
		}


		public override NSDragOperation DraggingEntered(NSDraggingInfo dragInfo)
		{
			if (CompleteDropAction == null)
			{
				return NSDragOperation.None;
			}
			var list = FilePaths(dragInfo);
			return list.Count > 0 ? NSDragOperation.Copy : NSDragOperation.None;
		}

		public override NSDragOperation DraggingUpdated(NSDraggingInfo sender)
		{
			if (CompleteDropAction == null)
			{
				return NSDragOperation.None;
			}
			return NSDragOperation.Copy;
		}

		public override bool PerformDragOperation(NSDraggingInfo dragInfo)
		{
			if (CompleteDropAction == null)
			{
				return false;
			}

			// Convert point to be relative to the map from window coordinates. Invert Y because the mapping
			// library is expecting 0 to be at the top, not the bottom
			var pt = ConvertPointFromView(new PointF(dragInfo.DraggingLocation.X, dragInfo.DraggingLocation.Y), null);
			pt.Y = Frame.Height - pt.Y;
			var ret = InvokeMapScript("pointToLatLng([{0}, {1}])", pt.X, pt.Y) as NSString;
			if (ret != null)
			{
				var json = JObject.Parse((string) ret);
				var lat = (double) json["lat"];
				var lng = (double) json["lng"];

				CompleteDropAction(lat, lng, FilePaths(dragInfo));
				return true;
			}

			logger.Error("pointToLatLng returned unexpected value: {0}", ret);
			return false;
		}

		IList<string> FilePaths(NSDraggingInfo dragInfo)
		{
			var list = new List<string>();
			if (dragInfo.DraggingPasteboard.Types.Contains(NSPasteboard.NSFilenamesType))
			{
				NSArray data = dragInfo.DraggingPasteboard.GetPropertyListForType(NSPasteboard.NSFilenamesType) as NSArray;
				if (data != null)
				{
					for (uint idx = 0; idx < data.Count; ++idx)
					{
						string path = NSString.FromHandle(data.ValueAt(idx));
						if (Directory.Exists(path) || File.Exists(path))
						{
							list.Add(path);
						}
					}
				}
			}

			return list;
		}
	}
}
