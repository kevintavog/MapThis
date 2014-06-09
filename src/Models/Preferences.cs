using System;
using NLog;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MapThis
{
	public class Preferences
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		static public Preferences Instance { get; private set; }


		//  Properties perisisted between sessions
		public string LastOpenedFolder { get; set; }
		public string LastSelectedFolder { get; set; }
		public int ImageFilterIndex { get; set; }


		// Properties derived from other properties
		public string KeywordFile { get { return Path.Combine(PreferencesFolder, "com.rangic.MapThis.Keywords.json"); } }
		public string PreferencesFolder { get { return Path.GetDirectoryName(Filename); } }


		private string Filename { get; set; }

		public void Save()
		{
			Save(Filename);
		}

		static public Preferences Load(string filename)
		{
			Instance = new Preferences();
			Instance.Filename = filename;
			if (File.Exists(filename))
			{
				try
				{
					dynamic json = JObject.Parse(File.ReadAllText(filename));

					Instance.LastOpenedFolder = json.LastOpenedFolder;
					Instance.LastSelectedFolder = json.LastSelectedFolder;
					Instance.ImageFilterIndex = json.ImageFilterIndex;
				}
				catch (Exception e)
				{
					logger.Error("Exception loading preferences (using defaults): {0}", e);
				}
			}
			else
			{
				try
				{
					Save(filename);
				}
				catch (Exception e)
				{
					logger.Error("Exception saving preferences to '{0}': {1}", filename, e);
				}
			}
			return Instance;
		}

		static private void Save(string filename)
		{
			var prefs = new
			{
				Instance.LastOpenedFolder,
				Instance.LastSelectedFolder,
				Instance.ImageFilterIndex,
			};

			File.WriteAllText(filename, JsonConvert.SerializeObject(prefs));
		}
	}
}

