using MonoMac.Foundation;
using MonoMac.AppKit;
using System.IO;
using NLog;

namespace MapThis.View
{
	public partial class AppDelegate : NSApplicationDelegate
	{
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();
		MainWindowController mainWindowController;


		public override void FinishedLaunching(NSObject notification)
		{
			var urlList = new NSFileManager().GetUrls(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User);

			Preferences.Load(Path.Combine(
				urlList[0].Path,
				"Preferences",
				"com.rangic.MapThis.json"));

            Rangic.Utilities.Geo.OpenStreetMapLookupProvider.UrlBaseAddress = Preferences.Instance.BaseLocationLookup;
            logger.Info("Resolving placenames via {0}", Rangic.Utilities.Geo.OpenStreetMapLookupProvider.UrlBaseAddress);

			mainWindowController = new MainWindowController();
			mainWindowController.Window.MakeKeyAndOrderFront(this);
		}

		public override bool OpenFile(NSApplication sender, string filename)
		{
			if (mainWindowController == null)
			{
				mainWindowController = new MainWindowController();
				mainWindowController.Window.MakeKeyAndOrderFront(this);
			}

			return mainWindowController.OpenFolderDirectly(filename);
		}
	}
}
