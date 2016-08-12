using POGOProtos.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.Model
{
    public class PokemonListModel
    {
        public PokemonId Pokemon { get; set; }
        public bool Tracking { get; set; }

        public PokemonListModel()
        {
        }

        public PokemonListModel(PokemonId pokemon)
        {
            Pokemon = pokemon;
        }
    }
}