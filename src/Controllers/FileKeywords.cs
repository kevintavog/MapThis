using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using NLog;
using Newtonsoft.Json.Linq;
using Rangic.Utilities.Process;

namespace MapThis
{
	public class FileKeywords
	{
		static private readonly Logger logger = LogManager.GetCurrentClassLogger();

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

        public string Load(Func<string, IEnumerable<string>> getKeywords)
		{
			try
			{
				foreach (var name in Filenames)
				{
                    var keywords = ToDictionary(getKeywords(name));
					fileToKeywords[name] = keywords;
					AddKeywords(keywords);
				}

				return null;
			}
			catch (Exception e)
			{
				logger.Error("Error getting keywords: {0}", e);
				return e.Message;
			}
			finally
			{
				GenerateCommonKeywords();
				CacheAllKeywords();
			}
		}

        private OrderedDictionary ToDictionary(IEnumerable<string> keywords)
        {
            var od = new OrderedDictionary();
            foreach (var k in keywords)
            {
                if (!od.Contains(k))
                {
                    od.Add(k, null);
                }
            }
            return od;
        }

		private void AddKeywords(OrderedDictionary keywords)
		{
			foreach (var k in keywords.Keys)
			{
				if (!allKeywords.Contains(k))
				{
					allKeywords.Add(k, null);
				}

				if (!originalKeywords.Contains(k))
				{
					originalKeywords.Add(k, null);
				}
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

		public bool Save(out string message)
		{
			message = null;
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
				return false;
			}

			try
			{
				var quotedFilenames = "\"" + String.Join("\" \"", Filenames) + "\"";

				var addCommand = new StringBuilder();
				foreach (var s in added)
				{
					addCommand.AppendFormat("\"-IPTC:Keywords+={0}\" \"-XMP:Subject+={0}\" ", s);
				}

				var removeCommand = new StringBuilder();
				foreach (var s in removedKeywords)
				{
					removeCommand.AppendFormat("\"-IPTC:Keywords-={0}\" \"-XMP:Subject-={0}\" ", s);
				}

                new ExifToolInvoker().Run("-P -overwrite_original {0} {1} {2}", addCommand, removeCommand, quotedFilenames);
				return true;
			}
			catch (Exception e)
			{
				logger.Error("Error saving keywords: {0}", e);
				message = e.Message;
				return false;
			}
		}
	}
}