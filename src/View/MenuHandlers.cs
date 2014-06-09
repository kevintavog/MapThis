using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MapThis.Models;

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

			directoryTree = new DirectoryTree(openPanel.Url.Path);
			directoryView.ReloadData();

			Preferences.Instance.LastOpenedFolder = openPanel.Url.Path;
			Preferences.Instance.Save();

			// That's gross - Mono exposes SharedDocumentController as NSObject rather than NSDocumentcontroller
			(NSDocumentController.SharedDocumentController as NSDocumentController).NoteNewRecentDocumentURL(openPanel.Url);
		}

	}

	public enum NsButtonId
	{
		Cancel = 0,
		OK = 1
	}
}
