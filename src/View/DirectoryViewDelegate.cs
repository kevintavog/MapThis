using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MapThis.Models;
using System.Collections.Generic;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		private HashSet<string> imageTypes = new HashSet<string>();

		private void ChangeFileTypes(HashSet<string> newTypes)
		{
			imageTypes = newTypes;
			PopulateImageView();
		}

		private void PopulateImageView()
		{
            var tree = ToTree(directoryView.ItemAtRow(directoryView.SelectedRow));
			logger.Info("Selection changed: {0}", tree.Path);
			Preferences.Instance.LastSelectedFolder = tree.Path;
			Preferences.Instance.Save();

            ClearAllMarkers();
			imageViewItems.Clear();
			foreach (var f in tree.Files)
			{
				NSError error;
				var fileType = NSWorkspace.SharedWorkspace.TypeOfFile(f.Path, out error);
				if (imageTypes.Contains(fileType))
				{
                    var item = new ImageViewItem(f.Path);
					int pos = imageViewItems.BinarySearch(item);
					if (pos < 0)
					{
						imageViewItems.Insert(~pos, item);
					}
				}
			}

			imageView.ReloadData();
			ConfigureImageViewTooltips();
            SetFolderStatus();
		}

		[Export("outlineViewSelectionDidChange:")]
		public void OutlineViewSelectionChanged(NSNotification note)
		{
			PopulateImageView();
		}

		[Export("outlineView:numberOfChildrenOfItem:")]
		public int OutlineViewNumberOfChildrenOfItem(NSOutlineView view, NSObject item)
		{
			return ToTree(item).Directories.Count;
		}

		[Export("outlineView:isItemExpandable:")]
		public bool OutlineViewIsItemExpandable(NSOutlineView view, NSObject item)
		{
			return ToTree(item).Directories.Count > 0;
		}

		[Export("outlineView:child:ofItem:")]
		public NSObject OutlineViewChildOfItem(NSOutlineView view, int childIndex, NSObject item)
		{
			return ToTree(item).Directories[childIndex];
		}

		[Export("outlineView:objectValueForTableColumn:byItem:")]
		public NSObject OutlineViewValueForColumn(NSOutlineView view, NSTableColumn column, NSObject item)
		{
			return (NSString) DirectoryTree.RelativePath(directoryTree, ToTree(item).Path);
		}

		private DirectoryTree ToTree(NSObject item)
		{
			if (item == null)
			{
				return directoryTree;
			}
			return (DirectoryTree) item;
		}
	}
}
