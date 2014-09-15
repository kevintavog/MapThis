using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using MonoMac.Foundation;

namespace MapThis.Models
{
	public class DirectoryTree : NSObject
	{
		static private readonly Logger logger = LogManager.GetCurrentClassLogger();
		private List<DirectoryTree> folders;
		private List<DirectoryTree> files;

		public string Path { get; private set; }

		public DirectoryTree(string folder)
		{
			Path = folder;
		}

		static public string RelativePath(DirectoryTree baseTree, string directory)
		{
			if (directory.StartsWith(baseTree.Path, StringComparison.CurrentCultureIgnoreCase))
			{
				var relative = directory.Substring(baseTree.Path.Length);
				if (relative.Length > 0 && relative[0] == System.IO.Path.DirectorySeparatorChar)
				{
					relative = relative.Substring(1);
				}
				return relative;
			}
			return directory;
		}

		public List<DirectoryTree> Directories
		{
			get
			{
				if (folders == null)
				{
					GetChildren();
				}
				return folders;
			}
		}

		public List<DirectoryTree> Files
		{
			get
			{
				if (files == null)
				{
					GetChildren();
				}
				return files;
			}
		}

		private void GetChildren()
		{
			var fileEntries = new List<DirectoryTree>();
			var folderEntries = new List<DirectoryTree>();
			try
			{
				foreach (var f in Directory.GetFiles(Path))
				{
					fileEntries.Add(new DirectoryTree(f));
				}

				foreach (var d in Directory.GetDirectories(Path))
				{
					folderEntries.Add(new DirectoryTree(d));
				}
			}
			catch (IOException e)
			{
				logger.Warn("Exception getting children for '{0}': {1}", Path, e);
			}

			folders = folderEntries;
			files = fileEntries;
		}
	}
}
