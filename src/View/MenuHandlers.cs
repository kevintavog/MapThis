using System;
using MonoMac.Foundation;

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
		}
	}
}

