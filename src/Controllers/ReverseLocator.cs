using System;
using NLog;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MapThis.Controllers
{
	static public class ReverseLocator
	{
		public enum Filter
		{
			None,
			Minimal,
			Standard,
		}

		static private readonly Logger logger = LogManager.GetCurrentClassLogger();

		static public string ToPlaceName(double latitude, double longitude, Filter filter)
		{
			var placeName = "";
			try
			{
				using (var client = new HttpClient())
				{
					var requestUrl = string.Format(
                        "nominatim/v1/reverse?format=json&lat={0}&lon={1}&addressdetails=1&zoom=18&accept-language=en-us",
						latitude,
						longitude);

					client.BaseAddress = new Uri("http://open.mapquestapi.com/");
					var result = client.GetAsync(requestUrl).Result;
					if (result.IsSuccessStatusCode)
					{
						// Parse the name out...
						var body = result.Content.ReadAsStringAsync().Result;
						dynamic response = JObject.Parse(body);
						if (response["error"] != null)
						{
							logger.Warn("GeoLocation error: {0}", response.error);
						}

						placeName = PlaceNameFromOsmResponse(response, filter);
						if (placeName == null)
						{
							placeName = "";
						}
					}
					else
					{
						logger.Warn("GeoLocation request failed: {0}; {1}; {1}", 
							result.StatusCode, 
							result.ReasonPhrase,
							result.Content.ReadAsStringAsync().Result);
					}
				}
			}
			catch (AggregateException ae)
			{
				logger.Warn("Exception getting geolocation:");
				foreach (var inner in ae.InnerExceptions)
				{
					logger.Warn("  {0}", inner.Message);
				}
			}
			catch (Exception e)
			{
				logger.Warn("Exception getting geolocation: {0}", e);
			}

			return placeName;
		}

		static private string PlaceNameFromOsmResponse(dynamic response, Filter filter)
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
				switch (filter)
				{
					case Filter.Standard:
						if (AcceptedKeys.Contains(kv.Name))
						{
							parts.Add((string) kv.Value);
						}
						if ("county" == kv.Name && response["address"]["city"] == null)
						{
							parts.Add((string) kv.Value);
						}
						break;

					case Filter.None:
						parts.Add((string) kv.Value);
						break;

					case Filter.Minimal:
						if ("country_code" != kv.Name)
						{
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
						break;
				}
			}

			return String.Join(", ", parts);
		}

		// This really ought to be configurable by the user. Even better if they could pick from the tags
		// found in their image set
		static private ISet<string> AcceptedKeys = new HashSet<string>
		{
			"attraction",
			"basin",
			"city",
			"country",
			"cycleway",
			"footway",
			"garden",
			"hamlet",
			"park",
			"path",
			"nature_reserve",
			"stadium",
			"state",
			"suburb",
		};
	}
}

