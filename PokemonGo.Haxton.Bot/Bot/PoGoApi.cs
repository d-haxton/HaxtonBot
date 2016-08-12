using Newtonsoft.Json.Linq;
using POGOProtos.Enums;
using POGOProtos.Map.Pokemon;
using PokemonGo.Haxton.Bot.Utilities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Bot
{
    public class PoGoApi
    {
        public string Token { get; set; }
        private RestClient Rc { get; } = new RestClient("http://haxton.io/");

        public PoGoApi(string token)
        {
            Token = token;
        }
    }
}