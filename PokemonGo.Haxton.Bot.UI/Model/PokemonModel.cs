using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.Model
{
    public class PokemonModel
    {
        public string Name { get; set; }
        public int CP { get; set; }
        public int MaxCP { get; set; }
        public double PerfectPercentage { get; set; }
        public int Level { get; set; }
        public int Candies { get; set; }
    }
}