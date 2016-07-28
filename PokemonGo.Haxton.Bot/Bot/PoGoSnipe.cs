using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Bot
{
    public interface IPoGoSnipe
    {
        ConcurrentBag<KeyValuePair<double, double>> SnipeLocations { get; }

        void AddNewSnipe(double x, double y);
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
    }
}