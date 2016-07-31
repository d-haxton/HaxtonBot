using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
        List<SnipeLocationInfo> FetchSnipeLocationsFromFeeder(CancellationToken _token);
    }

    public class PoGoSnipe : IPoGoSnipe
    {
        public ConcurrentBag<KeyValuePair<double, double>> SnipeLocations { get; }
        private List<SniperInfo> _snipeLocationInfo = new List<SniperInfo>();
        private bool _fetchTaskRunninig = false;

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
				double x = Convert.ToDouble(coords[0], CultureInfo.InvariantCulture);
				double y = Convert.ToDouble(coords[1], CultureInfo.InvariantCulture);
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

        public List<SnipeLocationInfo> FetchSnipeLocationsFromFeeder(CancellationToken _token)
        {
            if (_fetchTaskRunninig == false)
            {
                Task.Run(async () => {
                    await startFetchingLocations(_token);
                });
                _fetchTaskRunninig = true;
            }
            List<SnipeLocationInfo> _snipeLocations = new List<SnipeLocationInfo>();
            _snipeLocationInfo.ForEach((x) => {
                _snipeLocations.Add(new SnipeLocationInfo(x.Id, x.Id.ToString(), x.Latitude.ToString(CultureInfo.InvariantCulture) +','+ x.Longitude.ToString(CultureInfo.InvariantCulture), x.TimeStampAdded.ToUniversalTime().ToString(), null));
            });
            return _snipeLocations;
        }

        private async Task startFetchingLocations(CancellationToken _token)
        {
            _fetchTaskRunninig = true;
            _token.ThrowIfCancellationRequested();
            try
            {
                var lClient = new TcpClient();
                lClient.Connect("localhost",
                    16969);

                var sr = new StreamReader(lClient.GetStream());

                while (lClient.Connected && !_token.IsCancellationRequested)
                {
                    var line = sr.ReadLine();
                    if (line == null)
                        throw new Exception("Unable to ReadLine from sniper socket");
                    JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                    var info = jsonSerializer.Deserialize<SniperInfo>(line);

                    if (_snipeLocationInfo.Any(x =>
                        Math.Abs(x.Latitude - info.Latitude) < 0.0001 &&
                        Math.Abs(x.Longitude - info.Longitude) < 0.0001))
                        // we might have different precisions from other sources
                        continue;

                    _snipeLocationInfo.RemoveAll(x => DateTime.Now > x.TimeStampAdded.AddMinutes(15));
                    _snipeLocationInfo.Add(info);
                }
            }
            catch (SocketException)
            {
                // this is spammed to often. Maybe add it to debug log later
            }
            catch (Exception ex)
            {
                // most likely System.IO.IOException
                Debug.WriteLine($"Failed to fetch auto snipe locations: {ex}");
            }
            _fetchTaskRunninig = true;
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

        public SnipeLocationInfo(int id, string name, string coords, string until, string icon)
        {
            this.id = id;
            this.name = name;
            this.coords = coords;
            this.until = until;
            this.icon = icon;
        }
	}

	public class SnipeLocationsFromPokeSnipe
	{
		public List<SnipeLocationInfo> results { get; set; }
	}

    public class SniperInfo
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Iv { get; set; }
        public DateTime TimeStamp { get; set; }
        public int Id { get; set; }
        public DateTime TimeStampAdded { get; set; } = DateTime.Now;
    }
}