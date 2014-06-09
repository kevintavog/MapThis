using System;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace MapThis
{
	public class KeywordSet
	{
		private OrderedDictionary keywords = new OrderedDictionary 
		{ 
			{ "art", null },
			{ "dog", null },
			{ "fountain", null },
			{ "landscape", null },
			{ "macro", null }, 
			{ "mountain biking", null },
			{ "mural", null },
			{ "night", null },
			{ "Seattle", null },
			{ "sunset", null },
			{ "Troy", null },
		};

		public IList<string> AsList { get; private set; }
		public bool Contains(string word)
		{
			return keywords.Contains(word);
		}

		public KeywordSet()
		{
			UpdateCachedList();
		}

		public bool AddKeyword(string word)
		{
			if (!keywords.Contains(word))
			{
				keywords[word] = true;
				UpdateCachedList();
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
				return true;
			}

			return false;
		}

		private void UpdateCachedList()
		{
			var list = new List<string>();
			foreach (var keyword in keywords.Keys)
			{
				list.Add(keyword.ToString());
			}
			AsList = list.AsReadOnly();
		}
	}
}

