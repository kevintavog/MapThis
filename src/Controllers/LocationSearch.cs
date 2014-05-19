using System;
using NLog;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using MapThis.Models;
using System.Threading.Tasks;
using System.Threading;

namespace MapThis.Controllers
{
	public class LocationSearch : IDisposable
	{
		static private readonly Logger logger = LogManager.GetCurrentClassLogger();

		private bool endSearch;
		private Queue<string> searchQueue = new Queue<string>();
		Action<IEnumerable<SearchArea>> searchComplete;


		public LocationSearch(Action<IEnumerable<SearchArea>> searchComplete)
		{
			this.searchComplete = searchComplete;
			Task.Run( () => SearchTask() );
		}

		#region IDisposable implementation

		public void Dispose()
		{
			endSearch = true;
		}

		#endregion


		public void Search(string search)
		{
			if (String.IsNullOrWhiteSpace(search))
			{
				searchComplete(null);
				return;
			}

			search = PrepareSearchString(search);
			lock(searchQueue)
			{
				searchQueue.Enqueue(search);
			}
		}

		private string PrepareSearchString(string search)
		{
			return search.Trim();
		}

		private IEnumerable<SearchArea> OsmSearch(string search)
		{
			var areas = new List<SearchArea>();

			try
			{
				using (var client = new HttpClient())
				{
					int limit = search.Length > 3 ? 20 : 5;
					var requestUrl = string.Format(
						"nominatim/v1/search?q={0}&format=jsonv2&polygon=0&addressdetails=1&dedup=1&limit={1}",
						search.Replace(' ', '+'),
						limit);

					client.BaseAddress = new Uri("http://open.mapquestapi.com/");
					var result = client.GetAsync(requestUrl).Result;
					if (result.IsSuccessStatusCode)
					{
						var body = result.Content.ReadAsStringAsync().Result;
						dynamic response = JArray.Parse(body);
						foreach (var p in response)
						{
							var bb = p["boundingbox"];
							double lat = p["lat"];
							double lon = p["lon"];
							string placeName = PlaceName(p);

							areas.Add(new SearchArea(
								new Area(
									new Location((double)bb[0], (double)bb[2]), 
									new Location((double)bb[1], (double)bb[3]), 
									placeName),
								new Location(lat, lon)));
						}
					}
					else
					{
						logger.Warn("Search request failed: {0}; {1}; {2}", 
							result.StatusCode, 
							result.ReasonPhrase,
							result.Content.ReadAsStringAsync().Result);
					}
				}
			}
			catch (AggregateException ae)
			{
				logger.Warn("Exception searching:");
				foreach (var inner in ae.InnerExceptions)
				{
					logger.Warn("  {0}", inner.Message);
				}
			}
			catch (Exception e)
			{
				logger.Warn("Exception searching: {0}", e);
			}

			return areas;
		}

		private string PlaceName(dynamic response)
		{
			if (response["address"] == null)
			{
				if (response["display_name"] != null)
				{
					return response.display_name;
				}
				return null;
			}

			var parts = new List<string>();
			foreach (var kv in response["address"])
			{
				if ("country_code" == kv.Name)
				{
					continue;
				}
				if ("county" == kv.Name)
				{
					if (response["address"]["city"] == null)
					{
						parts.Add((string) kv.Value);
					}
				}
				else
				{
					parts.Add((string) kv.Value);
				}
			}

			return String.Join(", ", parts);
		}

		private void SearchTask()
		{
			string lastSearch = null;
			try
			{
				while (!endSearch)
				{
					string searchTerm = NextSearch();
					logger.Info("Starting search: '{0}'", searchTerm);

					if (searchTerm != null)
					{
						if (!searchTerm.Equals(lastSearch, StringComparison.CurrentCultureIgnoreCase))
						{
							lastSearch = searchTerm;
							var list = OsmSearch(searchTerm);
							searchComplete(list);
						}
					}
				}
			}
			finally
			{
				logger.Info("SearchTask exiting");
			}
		}

		private string NextSearch()
		{
			while (!endSearch)
			{
				if (searchQueue.Count > 0)
				{
					lock(searchQueue)
					{
						// Always return the last queued item, dumping any earlier items
						if (searchQueue.Count > 0)
						{
							string term = null;
							while (searchQueue.Count > 0)
							{
								term = searchQueue.Dequeue();
							}
							return term;
						}
					}
				}

				Thread.Sleep(100);
			}

			return null;
		}
	}
}
