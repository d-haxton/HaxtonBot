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
        private static CancellationTokenSource _cancelTokenSource;
        private static bool _LoggedIn = false;
        private static bool ShouldRun;
        private static List<Task> task = null;

        [HandleProcessCorruptedStateExceptions]
        private static void Main(string[] args)
        {
            _cancelTokenSource = new CancellationTokenSource();
            ShouldRun = true;
            while (ShouldRun)
            {
                try
                {
                    List<Task> currentTasks = new List<Task>();
                    _LoggedIn = false;
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
                        _.For<IPoGoAsh>().Use<PoGoAsh>().Singleton();
                        _.For<IPoGoPokemon>().Use<PoGoPokemon>().Singleton();
                        _.For<IPoGoPokestop>().Use<PoGoPokestop>().Singleton();
                        _.For<IPoGoBot>().Use<PoGoBot>().Singleton();
                        _.For<IPoGoInventory>().Use<PoGoInventory>().Ctor<CancellationToken>().Is(_cancelTokenSource.Token).Singleton();
                        _.For<IPoGoEncounter>().Use<PoGoEncounter>().Singleton();
                        _.For<IPoGoNavigation>().Use<PoGoNavigation>().Singleton();
                        _.For<IPoGoMap>().Use<PoGoMap>().Singleton();
                        _.For<IPoGoStatistics>().Use<PoGoStatistics>().Singleton();
                        _.For<IPoGoSnipe>().Use<PoGoSnipe>().Singleton();
                        _.For<IPoGoLogin>().Use<PoGoLogin>().Singleton();
                        _.For<IPoGoFort>().Use<PoGoFort>().Singleton();

                        _.For<ISettings>().Use<Settings>().Singleton();
                        _.For<ILogicSettings>().Use<LogicSettings>().Singleton();
                    });
                    currentTasks.Add(RunTask(_cancelTokenSource.Token));
                    while (_LoggedIn == false && !_cancelTokenSource.Token.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }
                    if (_LoggedIn == false)
                    {
                        logger.Warn("Failed to log in, retrying in 5 seconds");
                        Thread.Sleep(5000);
                    }
                    if (_cancelTokenSource.Token.IsCancellationRequested)
                    {
                        continue;
                    }
                    currentTasks.Add(Task.Run(async () => { await handleConsoleInput(_cancelTokenSource.Token); }, _cancelTokenSource.Token));
                    Task.WaitAny(currentTasks.ToArray());
                }
                catch (AggregateException e)
                {
                    logger.Fatal("\nAggregateException thrown with the following inner exceptions:");
                    // Display information about each exception. 
                    foreach (var v in e.InnerExceptions)
                    {
                        if (v is TaskCanceledException)
                            logger.Fatal("   TaskCanceledException: Task {0}",
                                              ((TaskCanceledException)v).Task.Id);
                        else
                            logger.Fatal("   Exception: {0}", v.GetType().Name);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    logger.Fatal(ex.ToString());
                }
                finally
                {
                    _cancelTokenSource.Cancel();
                    _cancelTokenSource.Dispose();
                    container.Dispose();
                    _cancelTokenSource = new CancellationTokenSource();
                }
            }
        }

        private static Task RunTask(CancellationToken _token)
        {
            return Task.Run(() =>
            {
                while (!_token.IsCancellationRequested)
                {
                    try
                    {
                        var login = container.GetInstance<IPoGoLogin>();
                        login.DoLogin();
                        var bot = container.GetInstance<IPoGoBot>();
                        task = bot.Run(_token);
                        task.Add(Task.Run(async () => { await UpdateConsole(_token); }, _token));
                        _LoggedIn = true;
                        Task.WaitAny(task.ToArray());
                    }
                    catch (AggregateException e)
                    {
                        logger.Fatal("\nAggregateException thrown with the following inner exceptions:");
                        // Display information about each exception. 
                        foreach (var v in e.InnerExceptions)
                        {
                            if (v is TaskCanceledException)
                                logger.Fatal("   TaskCanceledException: Task {0}",
                                                  ((TaskCanceledException)v).Task.Id);
                            else
                                logger.Fatal("   Exception: {0}", v.GetType().Name);
                        }
                    }
                    finally
                    {
                        logger.Fatal("Task crashed or cancelled");
                        // for the case some tasks crashed
                        if (_token.CanBeCanceled)
                        {
                            _cancelTokenSource.Cancel();
                        }
                        _token.ThrowIfCancellationRequested();
                    }
                }
            }, _token);
        }

        private static async Task UpdateConsole(CancellationToken _token)
        {
            var stats = container.GetInstance<IPoGoStatistics>();
            while (!_token.IsCancellationRequested)
            {
                System.Console.Title = stats.statistics();
                await Task.Delay(30000);
            }
            _token.ThrowIfCancellationRequested();
        }

        private static async Task handleConsoleInput(CancellationToken _token)
        {
            var snipe = container.GetAllInstances<IPoGoSnipe>().ToList();
            while (ShouldRun && !_cancelTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if( System.Console.KeyAvailable)
                    {
                        var input = System.Console.ReadLine();
                        if (input == "exit")
                        {
                            ShouldRun = false;
                            _cancelTokenSource.Cancel();
                            logger.Warn("Exiting...");
                            break;
                        }
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
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Exception reading from console: " + ex.Message);
                }
                await Task.Delay(100);
            }
            _token.ThrowIfCancellationRequested();
        }
    }
}