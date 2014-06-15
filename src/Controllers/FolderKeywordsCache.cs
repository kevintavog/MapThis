using System;
using MapThis.Utilities;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using NLog;
using System.Collections.Generic;

namespace MapThis
{
	public class FolderKeywordsCache
	{
		static private readonly Logger logger = LogManager.GetCurrentClassLogger();
		static private HashSet<string> keywordPropertyNames = new HashSet<string>() { "XMP:Subject", "IPTC:Keywords" };

		private IDictionary<string,OrderedDictionary> fileToKeywords = new Dictionary<string, OrderedDictionary>();


		public FolderKeywordsCache()
		{
		}

		public OrderedDictionary ForFile(string filename)
		{
			OrderedDictionary keywords;
			if (fileToKeywords.TryGetValue(filename, out keywords))
			{
				return keywords;
			}

			return new OrderedDictionary();
		}

		public bool Load(string folder)
		{
			fileToKeywords.Clear();
			try
			{
				var exifOutput = ExifToolInvoker.Run("-j -G -Subject -Keywords \"{0}\"", folder);
				if (!String.IsNullOrEmpty(exifOutput.OutputString))
				{
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
							logger.Warn("A filename wasn't returned by ExifTool");
							continue;
						}

						fileToKeywords[filename] = keywordList;
					}
				}

				return true;
			}
			catch (Exception e)
			{
				logger.Error("Error getting keywords: {0}", e);
			}

			return false;
		}

		private void AddKeyword(OrderedDictionary dictionary, string keyword)
		{
			if (!dictionary.Contains(keyword))
			{
				dictionary.Add(keyword, null);
			}
		}
	}
}
