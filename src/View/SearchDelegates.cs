using System;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Collections.Generic;
using MapThis.Models;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		private void MoveSearchResult(bool up)
		{
			int change = up ? -1 : 1;
			if (searchData.Count > 0)
			{
				int curRow = searchResults.SelectedRow;
				int newRow = curRow + change;
				newRow = Math.Max(0, newRow);
				if (newRow >= searchData.Count)
				{
					newRow = 0;
				}

				searchResults.SelectRow(newRow, false);
			}
		}

		internal void SearchTextChanged()
		{
			locationSearch.Search(searchField.StringValue);
		}

		internal void SearchComplete(IEnumerable<SearchArea> results)
		{
			searchData.Clear();
			if (null != results)
			{
				foreach (var a in results)
				{
					searchData.Add(a);
				}
			}
			searchResults.ReloadData();
		}

		private void ActivateSearchResult()
		{
			int curRow = Math.Max(searchResults.SelectedRow, 0);
			if (searchData.Count > 0 && curRow >= 0)
			{
				mapController.ActivateSearchResult(searchData[curRow]);
			}
		}

		private class SearchTextFieldDelegate : NSTextFieldDelegate
		{
			MainWindowController controller;

			public SearchTextFieldDelegate(MainWindowController controller)
			{
				this.controller = controller;
			}

			public override void Changed(NSNotification notification)
			{
				controller.SearchTextChanged();
			}

			public override bool DoCommandBySelector(NSControl control, NSTextView textView, MonoMac.ObjCRuntime.Selector commandSelector)
			{
				switch (commandSelector.Name)
				{
					case "moveDown:":
						controller.MoveSearchResult(false);
						return true;

					case "moveUp:":
						controller.MoveSearchResult(true);
						return true;

					case "insertNewline:":
						controller.ActivateSearchResult();
						return true;
				}

				return false;
			}
		}
	}
}
