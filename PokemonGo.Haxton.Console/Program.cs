using NLog;
using PokemonGo.Haxton.Bot;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Bot;
using PokemonGo.Haxton.Bot.Inventory;
using PokemonGo.Haxton.Bot.Login;
using PokemonGo.Haxton.Bot.Navigation;
using PokemonGo.Haxton.Bot.Utilities;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Console
{
    internal class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Container container;

        private static void Main(string[] args)
        {
            container = new Container(_ =>
            {
                _.For<IApiBaseRpc>().Use<ApiBaseRpc>().Singleton();
                _.For<IApiClient>().Use<ApiClient>().Singleton();
                _.For<IApiDownload>().Use<ApiDownload>().Singleton();
                _.For<IApiEncounter>().Use<ApiEncounter>().Singleton();
                _.For<IApiFort>().Use<ApiFort>().Singleton();
                _.For<IApiInventory>().Use<ApiInventory>().Singleton();
                _.For<IApiLogin>().Use<ApiLogin>().Singleton();
                _.For<IApiMap>().Use<ApiMap>().Singleton();
                _.For<IApiMisc>().Use<ApiMisc>().Singleton();
                _.For<IApiPlayer>().Use<ApiPlayer>().Singleton();

                _.For<IPoGoBot>().Use<PoGoBot>().Singleton();
                _.For<IPoGoInventory>().Use<PoGoInventory>().Singleton();
                _.For<IPoGoEncounter>().Use<PoGoEncounter>().Singleton();
                _.For<IPoGoNavigation>().Use<PoGoNavigation>().Singleton();
                _.For<IPoGoMap>().Use<PoGoMap>().Singleton();

                _.Scan(s =>
                {
                    s.SingleImplementationsOfInterface();
                    s.AssemblyContainingType<IApiClient>();
                    s.WithDefaultConventions();
                });
            });
            var test = container.GetInstance<ILogicSettings>();
            var x = test.EvolveAllPokemonAboveIv;

            var login = container.GetInstance<IPoGoLogin>();
            login.DoLogin();
            var bot = container.GetInstance<IPoGoBot>();
            Task.Run(() => bot.Run());
            Task.Run(UpdateConsole);
            System.Console.ReadLine();
        }

        private static async Task UpdateConsole()
        {
            var stats = container.GetInstance<IPoGoStatistics>();
            while (true)
            {
                System.Console.Title = stats.GetCurrentInfo() + " " + stats;
                await Task.Delay(30000);
            }
        }
    }
}