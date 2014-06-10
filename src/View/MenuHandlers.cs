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

			OpenFolderDirectly(openPanel.Url.Path);
		}

		[Export("viewFile:")]
		public void ViewFile(NSObject sender)
		{
			if (imageView.SelectionIndexes.Count >= 1)
			{
				var item = ImageAtIndex(imageView.SelectionIndexes.FirstIndex);
				if (false == NSWorkspace.SharedWorkspace.OpenFile(item.File))
				{
					logger.Error("Unable to open '{0}'", item.File);
				}
			}
		}

		public bool OpenFolderDirectly(string path)
		{
			directoryTree = new DirectoryTree(path);
			directoryView.ReloadData();
			directoryView.SelectRow(0, false);

			Preferences.Instance.LastOpenedFolder = path;
			Preferences.Instance.Save();

			// That's gross - Mono exposes SharedDocumentController as NSObject rather than NSDocumentcontroller
			(NSDocumentController.SharedDocumentController as NSDocumentController).NoteNewRecentDocumentURL(new NSUrl(path, true));
			return true;
		}

	}

	public enum NsButtonId
	{
		Cancel = 0,
		OK = 1
	}
}
