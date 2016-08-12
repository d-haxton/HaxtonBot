using Microsoft.Practices.ServiceLocation;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.IoC
{
    public class StructuremapServiceLocator : IServiceLocator
    {
        private IContainer container;

        public StructuremapServiceLocator(IContainer container)
        {
            this.container = container;
        }

        public object GetService(Type serviceType)
        {
            return container.GetInstance(serviceType);
        }

        public object GetInstance(Type serviceType)
        {
            return container.GetInstance(serviceType);
        }

        public object GetInstance(Type serviceType, string key)
        {
            return container.GetInstance(serviceType, key);
        }

        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return container.GetAllInstances(serviceType).Cast<object>();
        }

        public TService GetInstance<TService>()
        {
            return container.GetInstance<TService>();
        }

        public TService GetInstance<TService>(string key)
        {
            return container.GetInstance<TService>(key);
        }

        public IEnumerable<TService> GetAllInstances<TService>()
        {
            return container.GetAllInstances<TService>();
        }
    }
}