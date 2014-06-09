﻿using System;
using System.Linq;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Collections.Generic;
using MonoMac.ObjCRuntime;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		private FileKeywords fileKeywords;
		private KeywordSet keywordSet = new KeywordSet();
		private float maxColumnWidth;


		[Export("numberOfRowsInTableView:")]
		public int numberOfRowsInTableView(NSTableView tv)
		{
			switch (tv.Tag)
			{
				case 1:
					return searchData.Count;
				case 2:
					if (fileKeywords != null)
					{
						var count = RowCountForList(tv, fileKeywords.AllKeywords.Count());
						return count;
					}
					return 0;
				case 3:
					return RowCountForList(tv, keywordSet.AsList.Count);

				default:
					logger.Info("numberOfRowsInTable for {0}", tv.Tag);
					return -1;
			}
		}

		private int RowCountForList(NSTableView tv, int listCount)
		{
			return listCount / tv.ColumnCount + ((listCount % tv.ColumnCount) == 0 ? 0 : 1);
		}

		[Export("tableView:objectValueForTableColumn:row:")]
		public NSObject objectValueForTableColumn(NSTableView table, NSTableColumn column, int rowIndex)
		{
			switch (table.Tag)
			{
				case 1:
					return (NSString) searchData[rowIndex].PlaceName;
				case 2:
					if (fileKeywords != null)
					{
						return ValueForRowColumn(fileKeywords.AllKeywords, table, rowIndex, column);
					}
					return null;
				case 3:
					return ValueForRowColumn(keywordSet.AsList, table, rowIndex, column);
				default:
					logger.Info("objectValueForTableColumn for {0}", table.Tag);
					return (NSString) ("Unknown tag " + table.Tag);
			}
		}

		private NSButtonCell ValueForRowColumn(IList<string> data, NSTableView tv, int row, NSTableColumn column)
		{
			int columnIndex = ColumnIndex(tv, column);
			NSButtonCell cell = (NSButtonCell) column.DataCell;

			int listIndex = (row * tv.ColumnCount) + columnIndex;
			if (listIndex >= data.Count)
			{
				cell.Transparent = true;
			}
			else
			{
				cell.Title = data[listIndex];
				cell.Transparent = false;

				if (tv.Tag == 3 && fileKeywords != null)
				{
					cell.State = fileKeywords.Contains(data[listIndex]) ? NSCellStateValue.On : NSCellStateValue.Off;
				}

				if (tv.Tag == 2 && fileKeywords.IsCommon(data[listIndex]))
				{
					cell.Title = data[listIndex] + " *";
				}
			}

			return cell;
		}

		private int ColumnIndex(NSTableView table, NSTableColumn column)
		{
			var tableColumns = table.TableColumns();
			for (int idx = 0; idx < tableColumns.Length; ++idx)
			{
				if (tableColumns[idx] == column)
				{
					return idx;
				}
			}
			return -1;
		}

		[Export ("draggingEntered:")]
		public NSDragOperation DraggingEntered(NSDraggingInfo sender)
		{
			logger.Info("draggingEntered");
			return false ? NSDragOperation.All : NSDragOperation.None;
		}


		private void KeywordFilesChanged(IList<string> selectedFiles)
		{
			if (fileKeywords != null)
			{
				string error = fileKeywords.Save();
				if (error != null)
				{
					NSAlert.WithMessage(error, "Close", "", "", "").RunSheetModal(Window);
				}
			}

			if (selectedFiles.Count == 0)
			{
				fileKeywords = null;
			}
			else
			{
				var keywordLoader = new FileKeywords(selectedFiles);
				keywordLoader.Load();
				fileKeywords = keywordLoader;
			}

			imageKeywords.ReloadData();
			allKeywords.ReloadData();

			maxColumnWidth = CalculateCellWidth();
			SetTableColumns();
		}

		private float CalculateCellWidth()
		{
			var biggestWidth = 0f;
			for (var column = 0; column < allKeywords.TableColumns().Length; ++column)
			{
				for (int row = 0; row < allKeywords.RowCount; ++row)
				{
					var cellWidth = allKeywords.GetCell(column, row).CellSize.Width;
					biggestWidth = Math.Max(biggestWidth, cellWidth);
				}
			}

			for (var column = 0; column < imageKeywords.TableColumns().Length; ++column)
			{
				for (int row = 0; row < imageKeywords.RowCount; ++row)
				{
					var cellWidth = imageKeywords.GetCell(column, row).CellSize.Width;
					biggestWidth = Math.Max(biggestWidth, cellWidth);
				}
			}

			for (var column = 0; column < allKeywords.TableColumns().Length; ++column)
			{
				var c = allKeywords.TableColumns()[column];
				c.Width = c.MaxWidth = biggestWidth;
			}

			for (var column = 0; column < imageKeywords.TableColumns().Length; ++column)
			{
				var c = imageKeywords.TableColumns()[column];
				c.Width = c.MaxWidth = biggestWidth;
			}

			return biggestWidth;
		}

		private void SetTableColumns()
		{
			if (maxColumnWidth < 1)
			{
				maxColumnWidth = CalculateCellWidth();
			}

			SetTableColumns(allKeywords);
			SetTableColumns(imageKeywords);
		}

		private void SetTableColumns(NSTableView tableView)
		{
			// Always keep at least one column
			int numColumns = (int) (tableView.EnclosingScrollView.Frame.Width / maxColumnWidth);
			if (numColumns < 1)
			{
				numColumns = 1;
			}

			int existingColumnCount = tableView.TableColumns().Count();
			if (numColumns > existingColumnCount)
			{
				logger.Info("Add columns; {0} to {1}", existingColumnCount, numColumns);
				while (tableView.TableColumns().Count() < numColumns)
				{
					var copy =  new NSTableColumn();
					copy.Width = copy.MaxWidth = maxColumnWidth;

					NSTableColumn column = tableView.TableColumns().First();
					copy.DataCell = (NSCell) column.DataCell.Copy();
					tableView.AddColumn(copy);
				}
				tableView.ReloadData();
			}
			else
			if (numColumns < existingColumnCount)
			{
				logger.Info("Remove columns; {0} to {1}", existingColumnCount, numColumns);
				while (tableView.TableColumns().Count() > numColumns)
				{
					tableView.RemoveColumn(tableView.TableColumns().Last());
				}
				tableView.ReloadData();
			}
		}

		partial void AllKeywordClick(NSObject sender)
		{
			int row = allKeywords.ClickedRow;
			int col = allKeywords.ClickedColumn;
			int listIndex = allKeywords.TableColumns().Count() * row + col;

			var keyword = keywordSet.AsList[listIndex];
			if (fileKeywords.Contains(keyword))
			{
				fileKeywords.Remove(keyword);
				imageKeywords.ReloadData();
			}
			else
			{
				fileKeywords.Add(keyword);
				AddedItemToTableView(imageKeywords, fileKeywords.AllKeywords);
			}
		}

		partial void ImageKeywordClick(NSObject sender)
		{
			int row = imageKeywords.ClickedRow;
			int col = imageKeywords.ClickedColumn;
			int listIndex = imageKeywords.TableColumns().Count() * row + col;

			var keyword = fileKeywords.AllKeywords[listIndex];
			fileKeywords.Remove(keyword);
		}

		private string[] GetCompletions()
		{
			logger.Info("get completions");
			return keywordSet.AsList.ToArray();
		}

		bool isCompleting;
		private void KeywordsTextChanged(NSNotification notification)
		{
			if (fileKeywords == null)
			{
				return;
			}

			if (!isCompleting)
			{
				isCompleting = true;
//				var fieldEditor = (NSTextView)notification.UserInfo.ObjectForKey((NSString)"NSFieldEditor");
//				fieldEditor.Complete(null);
				isCompleting = false;
			}
		}

		private void ApplyKeyword()
		{
			if (fileKeywords == null)
			{
				return;
			}

			var newKeyword = keywordEntry.StringValue.Trim();
			if (!String.IsNullOrEmpty(newKeyword))
			{
				if (!fileKeywords.Contains(newKeyword))
				{
					fileKeywords.Add(newKeyword);
					AddedItemToTableView(imageKeywords, fileKeywords.AllKeywords);
				}

				if (!keywordSet.Contains(newKeyword))
				{
					keywordSet.AddKeyword(newKeyword);
					AddedItemToTableView(allKeywords, keywordSet.AsList);
				}
			}

			keywordEntry.StringValue = "";
		}

		private void AddedItemToTableView(NSTableView tableView, IList<string> dataList)
		{
			int listCount = dataList.Count - 1;
			int row = listCount / tableView.ColumnCount;
			int col = listCount % tableView.ColumnCount;

			int tableRowCount = RowCountForList(tableView, listCount + 1);
			if (tableRowCount != tableView.RowCount)
			{
				tableView.BeginUpdates();
				tableView.InsertRows(new NSIndexSet(row), NSTableViewAnimation.None);
				tableView.EndUpdates();
			}
			else
			{
				tableView.ReloadData(new NSIndexSet(row), new NSIndexSet(col));
			}
		}

		private class KeywordsTextFieldDelegate : NSTextFieldDelegate
		{
			MainWindowController controller;

			public KeywordsTextFieldDelegate(MainWindowController controller)
			{
				this.controller = controller;
			}

			public override void Changed(NSNotification notification)
			{
				controller.KeywordsTextChanged(notification);
			}

			public override void EditingEnded(NSNotification notification)
			{
				controller.ApplyKeyword();
			}

			public override string[] GetCompletions(NSControl control, NSTextView textView, string[] words, NSRange charRange, int index)
			{
				return controller.GetCompletions();
			}
		}
	}
}

