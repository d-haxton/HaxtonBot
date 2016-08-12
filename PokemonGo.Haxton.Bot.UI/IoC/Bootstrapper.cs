using Microsoft.Practices.ServiceLocation;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.IoC
{
    internal class Bootstrapper
    {
        private static IContainer _theContainer;
        public static IContainer TheContainer => _theContainer ?? (_theContainer = GetContainer());

        private static IServiceLocator _theServiceLocator;
        public static IServiceLocator TheServiceLocator => _theServiceLocator ?? (_theServiceLocator = new StructuremapServiceLocator(TheContainer));

        private static Container GetContainer()
        {
            return new Container(new PokemonGoBotRegistry());
        }
    }
}