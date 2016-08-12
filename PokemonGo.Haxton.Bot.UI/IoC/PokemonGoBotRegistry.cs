using PokemonGo.Haxton.Bot.UI.Provider;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.IoC
{
    public class PokemonGoBotRegistry : Registry
    {
        public PokemonGoBotRegistry()
        {
            For<IAccountProvider>().Use<AccountProvider>().Singleton();
        }
    }
}