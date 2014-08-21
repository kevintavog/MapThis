using System;
using MapThis.Models;
using NLog;
using System.Threading.Tasks;

namespace MapThis.Controllers
{
	public class MapController
	{
		public delegate void ScriptInvokerAction(string script, params object[] args);


		private ScriptInvokerAction InvokeMapScript { get; set; }

		public MapController(ScriptInvokerAction invokeMapScript)
		{
			InvokeMapScript = invokeMapScript;
		}

		public void ActivateLocation(Location location)
		{
			InvokeMapScript("setCenter([{0}, {1}], 18)", location.Latitude, location.Longitude);
		}

		public void ActivateSearchResult(SearchArea sa)
		{
			if (Math.Abs(sa.Left - sa.Right) < 0.001 || Math.Abs(sa.Top - sa.Bottom) < 0.001)
			{
				ActivateLocation(sa.Location);
			}
			else
			{
				InvokeMapScript("fitToBounds([[{0}, {1}],[{2}, {3}]])", sa.Bottom, sa.Left, sa.Top, sa.Right);
			}
		}

        static public void NameFromLocation(Location location, ReverseLocator.Filter filter, Action<string> action)
        {
            NameFromLocation(location.Latitude, location.Longitude, filter, action);
        }

		static public void NameFromLocation(double latitude, double longitude, ReverseLocator.Filter filter, Action<string> action)
		{
			Task.Run( delegate ()
				{
					string name = ReverseLocator.ToPlaceName(latitude, longitude, filter);
					action(name);
				});

		}
	}
}
