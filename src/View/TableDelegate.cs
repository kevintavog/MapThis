using System;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		[Export("numberOfRowsInTableView:")]
		public int numberOfRowsInTableView(NSTableView tv)
		{
			return searchData.Count;
		}

		[Export("tableView:objectValueForTableColumn:row:")]
		public string objectValueForTableColumn(NSTableView table, NSTableColumn column, int rowIndex)
		{
			return searchData[rowIndex].PlaceName;
		}

		[Export ("draggingEntered:")]
		public NSDragOperation DraggingEntered(NSDraggingInfo sender)
		{
			logger.Info("draggingEntered");
			return false ? NSDragOperation.All : NSDragOperation.None;
		}
	}
}

