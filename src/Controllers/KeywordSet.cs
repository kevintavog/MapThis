using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace MapThis
{
	public class KeywordSet
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private SortedSet<string> keywords = new SortedSet<string>();

		public IList<string> AsList { get; private set; }
		public bool Contains(string word)
		{
			return keywords.Contains(word);
		}

		public KeywordSet()
		{
			if (File.Exists(Preferences.Instance.KeywordFile))
			{
				var json = JArray.Parse(File.ReadAllText(Preferences.Instance.KeywordFile));
				foreach (var item in json)
				{
					keywords.Add(item.ToString());
				}
			}

			UpdateCachedList();
		}

		public string[] GetMatches(string text)
		{
			text = text.Trim();
			var list = new List<string>();
			foreach (var k in AsList)
			{
				if (k.StartsWith(text, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(k);
				}
			}

			return list.ToArray();
		}

		public bool AddKeyword(string word)
		{
			if (!keywords.Contains(word))
			{
				keywords.Add(word);
				UpdateCachedList();
				SaveKeywords();
				return true;
			}

			return false;
		}

		public bool RemoveKeyword(string word)
		{
			if (keywords.Contains(word))
			{
				keywords.Remove(word);
				UpdateCachedList();
				SaveKeywords();
				return true;
			}

			return false;
		}

		private void SaveKeywords()
		{
			try
			{
				File.WriteAllText(Preferences.Instance.KeywordFile, JsonConvert.SerializeObject(new List<string>(keywords)));
			}
			catch (Exception e)
			{
                logger.Info("Error saving keywords: {0}", e.ToString());
			}
		}

		private void UpdateCachedList()
		{
			AsList = new List<string>(keywords);
		}
	}
}
