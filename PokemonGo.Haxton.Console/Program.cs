using NLog;
using PokemonGo.Haxton.Bot;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Bot;
using PokemonGo.Haxton.Bot.Inventory;
using PokemonGo.Haxton.Bot.Login;
using PokemonGo.Haxton.Bot.Navigation;
using PokemonGo.Haxton.Bot.Settings;
using PokemonGo.Haxton.Bot.Utilities;
using PokemonGo.RocketAPI;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Console
{
    internal class Program
    {
        private static Container container;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static bool ShouldRun;
        private static bool _LoggedIn = false;
        private static CancellationTokenSource _cancelToken;
        private static List<Task> task = null;

        [HandleProcessCorruptedStateExceptions]
        private static void Main(string[] args)
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            _cancelToken = new CancellationTokenSource();

            ShouldRun = true;
            RunTask();
            while (_LoggedIn == false)
            {
                Thread.Sleep(100);
            }
            var snipe = container.GetAllInstances<IPoGoSnipe>().ToList();
            while (ShouldRun)
            {
                try
                {
                    var input = System.Console.ReadLine();
                    if (input == "exit")
                        ShouldRun = false;
                    var split = input?.Split(',');
                    if (split != null)
                    {
                        var x = double.Parse(split[0], CultureInfo.InvariantCulture);
                        var y = double.Parse(split[1], CultureInfo.InvariantCulture);
                        foreach (var poGoSnipe in snipe)
                        {
                            poGoSnipe.AddNewSnipe(x, y);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Exception reading from console: " + ex.Message);
                }
            }
        }

        private static void RunTask()
        {
            Task.Run(() =>
            {
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
                while (true)
                {
                    try
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
                            _.For<IPoGoStatistics>().Use<PoGoStatistics>().Singleton();
                            _.For<IPoGoSnipe>().Use<PoGoSnipe>().Singleton();
                            _.For<IPoGoLogin>().Use<PoGoLogin>().Singleton();
                            _.For<IPoGoFort>().Use<PoGoFort>().Singleton();

                            _.For<ISettings>().Use<Settings>().Singleton();
                            _.For<ILogicSettings>().Use<LogicSettings>().Singleton();

                            //_.Scan(s =>
                            //{
                            //    s.SingleImplementationsOfInterface();
                            //    s.AssemblyContainingType<IApiClient>();
                            //    s.WithDefaultConventions();
                            //});
                        });

                        var login = container.GetInstance<IPoGoLogin>();
                        login.DoLogin();
                        var bot = container.GetInstance<IPoGoBot>();
                        task = bot.Run();
                        Task.Run(UpdateConsole);
                        _LoggedIn = true;
                        Task.WaitAny(task.ToArray());
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal(ex, "Fatal error, attempting to restart");
                    }
                }
            }, _cancelToken.Token);
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            task?.ForEach(x => x.Dispose());
            _cancelToken.Cancel();
            _cancelToken = new CancellationTokenSource();
            RunTask();
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