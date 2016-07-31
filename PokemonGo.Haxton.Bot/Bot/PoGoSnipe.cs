using NLog;
using POGOProtos.Networking.Requests.Messages;
using PokemonGo.Haxton.Bot.Navigation;
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

        Task DoSnipe();
    }

    public class PoGoSnipe : IPoGoSnipe
    {
        private readonly IPoGoNavigation _navigation;
        private readonly IPoGoAsh _ash;
        public ConcurrentBag<KeyValuePair<double, double>> SnipeLocations { get; }
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PoGoSnipe(IPoGoNavigation navigation, IPoGoAsh ash)
        {
            _navigation = navigation;
            _ash = ash;
            SnipeLocations = new ConcurrentBag<KeyValuePair<double, double>>();
        }

        public void AddNewSnipe(double x, double y)
        {
            SnipeLocations.Add(new KeyValuePair<double, double>(x, y));
        }

        public async Task DoSnipe()
        {
            try
            {
                var loc = GetNextSnipeLocation;
                logger.Warn($"Sniping at {loc.Key}, {loc.Value}");
                var startX = _navigation.CurrentLatitude;
                var startY = _navigation.CurrentLongitude;
                var funcs = await _ash.BurstCatch(loc.Key, loc.Value, 0, 0);
                await _navigation.TeleportToLocation(startX, startY);
                foreach (var act in funcs)
                {
                    act.Invoke();
                    await Task.Delay(1500);
                }
            }
            catch (ArgumentException)
            {
                // warn
            }
        }

        public bool HasSnipeLocation => SnipeLocations.IsEmpty == false;

        public KeyValuePair<double, double> GetNextSnipeLocation
        {
            get
            {
                KeyValuePair<double, double> loc;
                if (SnipeLocations.TryTake(out loc))
                {
                    return loc;
                }
                throw new ArgumentException("No valid snipe locations");
            }
        }
    }
}