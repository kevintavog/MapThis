using System;
using MapThis.Utilities;
using NLog;
using System.Collections.Generic;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections;
using System.Text;

namespace MapThis
{
	public class FileKeywords
	{
		static private readonly Logger logger = LogManager.GetCurrentClassLogger();

		static private HashSet<string> keywordPropertyNames = new HashSet<string>() { "XMP:Subject", "IPTC:Keywords" };

		public IEnumerable<string> Filenames { get; private set; }
		public IList<string> AllKeywords { get; private set; }

		private OrderedDictionary allKeywords = new OrderedDictionary();
		private OrderedDictionary originalKeywords = new OrderedDictionary();
		private OrderedDictionary commonKeywords = new OrderedDictionary();
		private IDictionary<string,OrderedDictionary> fileToKeywords = new Dictionary<string, OrderedDictionary>();
		private HashSet<string> removedKeywords = new HashSet<string>();

		public FileKeywords(IEnumerable<string> filenames)
		{
			Filenames = filenames;
		}

		public bool IsCommon(string keyword)
		{
			if (Filenames.Count() > 1)
			{
				return commonKeywords.Contains(keyword);
			}
			return false;
		}

		public bool Contains(string keyword)
		{
			return allKeywords.Contains(keyword);
		}

		public void Add(string keyword)
		{
			if (!allKeywords.Contains(keyword))
			{
				commonKeywords.Add(keyword, null);
				allKeywords.Add(keyword, true);
				CacheAllKeywords();

				if (removedKeywords.Contains(keyword))
				{
					removedKeywords.Remove(keyword);
				}
			}
		}

		public void Remove(string keyword)
		{
			if (allKeywords.Contains(keyword))
			{
				if (originalKeywords.Contains(keyword))
				{
					removedKeywords.Add(keyword);
				}

				allKeywords.Remove(keyword);
				commonKeywords.Remove(keyword);
				CacheAllKeywords();
			}
		}

		public void Load()
		{
			try
			{
				var quotedFilenames = "\"" + String.Join("\" \"", Filenames) + "\"";
				var exifOutput = ExifToolInvoker.Run("-j -G -Subject -Keywords {0}", quotedFilenames);
				var output = JArray.Parse(exifOutput.OutputString);
				for (int fileIndex = 0; fileIndex < output.Count; ++fileIndex)
				{
					var keywordList = new OrderedDictionary();
					string filename = null;
					foreach (var child in output[fileIndex].Children())
					{
						var prop = child as JProperty;
						if (prop != null)
						{
							if (prop.Name == "SourceFile")
							{
								filename = prop.Value.ToString();
							}
							if (!keywordPropertyNames.Contains(prop.Name))
							{
								continue;
							}

							var jarray = prop.Value as JArray;
							if (jarray != null)
							{
								foreach (var item in jarray.Values())
								{
									AddKeyword(keywordList, item.ToString());
								}
							}
							else
							{
								AddKeyword(keywordList, prop.Value.ToString());
							}
						}
					}

					if (filename == null)
					{
						logger.Warn("A filename wasn't returned by ExifTools");
						continue;
					}

					fileToKeywords[filename] = keywordList;
				}

				GenerateCommonKeywords();
				CacheAllKeywords();
			}
			catch (Exception e)
			{
				logger.Error("Error getting keywords: {0}", e);
				throw;
			}
		}

		private void AddKeyword(OrderedDictionary dictionary, string keyword)
		{
			if (!dictionary.Contains(keyword))
			{
				dictionary.Add(keyword, null);
			}

			if (!allKeywords.Contains(keyword))
			{
				allKeywords.Add(keyword, null);
			}

			if (!originalKeywords.Contains(keyword))
			{
				originalKeywords.Add(keyword, null);
			}
		}

		private void CacheAllKeywords()
		{
			var list = new List<string>();
			foreach (var keyword in allKeywords.Keys)
			{
				list.Add(keyword.ToString());
			}
			AllKeywords = list.AsReadOnly();
		}

		private void GenerateCommonKeywords()
		{
			foreach (var keyword in allKeywords.Keys)
			{
				bool missing = false;
				foreach (var kv in fileToKeywords)
				{
					if (!kv.Value.Contains(keyword))
					{
						missing = true;
						break;
					}
				}

				if (!missing)
				{
					commonKeywords.Add(keyword, null);
				}
			}
		}

		public string Save()
		{
			var added = new List<string>();
			foreach (DictionaryEntry de in allKeywords)
			{
				if (de.Value != null)
				{
					added.Add((string) de.Key);
				}
			}

			if (added.Count == 0 && removedKeywords.Count == 0)
			{
				return null;
			}

			try
			{
				var quotedFilenames = "\"" + String.Join("\" \"", Filenames) + "\"";

				var addCommand = new StringBuilder();
				foreach (var s in added)
				{
					addCommand.AppendFormat("-IPTC:Keywords+={0} -XMP:Subject+={0} ", s);
				}

				var removeCommand = new StringBuilder();
				foreach (var s in removedKeywords)
				{
					removeCommand.AppendFormat("-IPTC:Keywords-={0} -XMP:Subject-={0} ", s);
				}

				ExifToolInvoker.Run("-P -overwrite_original {0} {1} {2}", addCommand, removeCommand, quotedFilenames);
				return null;
			}
			catch (Exception e)
			{
				logger.Error("Error saving keywords: {0}", e);
				return e.Message;
			}
		}
	}
}