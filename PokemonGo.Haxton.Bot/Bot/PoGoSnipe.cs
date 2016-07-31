using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PokemonGo.Haxton.Bot.Bot
{
    public interface IPoGoSnipe
    {
        ConcurrentBag<KeyValuePair<double, double>> SnipeLocations { get; }
        void AddNewSnipe(double x, double y);
		void AddNewSnipe(string[] coords);
		Task<List<SnipeLocationInfo>> FetchSnipeLocations();
	}

    public class PoGoSnipe : IPoGoSnipe
    {
        public ConcurrentBag<KeyValuePair<double, double>> SnipeLocations { get; }

        public PoGoSnipe()
        {
            SnipeLocations = new ConcurrentBag<KeyValuePair<double, double>>();
        }

        public void AddNewSnipe(double x, double y)
        {
            SnipeLocations.Add(new KeyValuePair<double, double>(x, y));
        }

		public void AddNewSnipe(string[] coords) {
			if (coords.Length > 1)
			{
				double x = Convert.ToDouble(coords[0]);
				double y = Convert.ToDouble(coords[1]);
				AddNewSnipe(x, y);
            }
		}

		public async Task<List<SnipeLocationInfo>> FetchSnipeLocations()
		{
			try
			{
				string pokeSnipeJSON = new WebClient().DownloadString("http://pokesnipers.com/api/v1/pokemon.json");
				await Task.Delay(500);
				JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
				SnipeLocationsFromPokeSnipe snipeLocationsFound = jsonSerializer.Deserialize<SnipeLocationsFromPokeSnipe>(pokeSnipeJSON);

				return snipeLocationsFound.results;
			}
			catch (WebException ex)
			{
				 Debug.WriteLine($"Failed to fetch auto snipe locations: {ex}");
			}

			return null;
        }
	}

	public class SnipeLocationInfo
	{
		public int id { get; set; }
		public string name { get; set; }
		public string coords { get; set; }
		public string until { get; set; }
		public string icon { get; set; }
		
		public string UniqueId
		{
			get { return name + ":" + until;}
		}
	}

	public class SnipeLocationsFromPokeSnipe
	{
		public List<SnipeLocationInfo> results { get; set; }
	}


}