using NLog;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Navigation;
using PokemonGo.Haxton.Bot.Utilities;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Bot
{
    public interface IPoGoPokestop
    {
        IEnumerable<FortData> Pokestops { get; }

        Task Search(FortData pokestop);
    }

    public class PoGoPokestop : IPoGoPokestop
    {
        private readonly IPoGoFort _fort;
        private readonly IApiMap _map;
        private readonly ISettings _settings;
        private readonly IPoGoNavigation _navigation;
        private readonly ILogicSettings _logicSettings;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PoGoPokestop(IPoGoFort fort, IApiMap map, ISettings settings, IPoGoNavigation navigation, ILogicSettings logicSettings)
        {
            _fort = fort;
            _map = map;
            _settings = settings;
            _navigation = navigation;
            _logicSettings = logicSettings;
        }

        private async Task RemoveSoftBan(FortData closestPokestop)
        {
            var pokestopBooty = await _fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
            while (pokestopBooty.Result == FortSearchResponse.Types.Result.Success)
            {
                pokestopBooty = await _fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
            }
            logger.Info("Softban removed.");
        }

        public IEnumerable<FortData> Pokestops
        {
            get
            {
                var mapObjects = _map.GetMapObjects().GetAwaiter().GetResult();
                var pokeStops = mapObjects.MapCells
                    .SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Checkpoint &&
                            i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    _settings.DefaultLatitude, _settings.DefaultLongitude,
                                    i.Latitude, i.Longitude) < _logicSettings.MaxTravelDistanceInMeters) ||
                            _logicSettings.MaxTravelDistanceInMeters == 0
                    )
                    .OrderBy(i => LocationUtils.CalculateDistanceInMeters(_navigation.CurrentLatitude,
                            _navigation.CurrentLongitude, i.Latitude, i.Longitude));
                //logger.Info($"{pokeStops.Count()} pokestops found.");
                return pokeStops;
            }
        }

        public async Task Search(FortData pokestop)
        {
            var pokestopBooty =
                await _fort.SearchFort(pokestop.Id, pokestop.Latitude, pokestop.Longitude);
            if (pokestopBooty.ExperienceAwarded > 0)
            {
                logger.Info(
                    $"Pokestop rewarded us with {pokestopBooty.ExperienceAwarded} exp. {pokestopBooty.GemsAwarded} gems. {StringUtils.GetSummedFriendlyNameOfItemAwardList(pokestopBooty.ItemsAwarded)}.");
            }
            else
            {
                logger.Info("Possible softban detected, attempting to remove.");
                await RemoveSoftBan(pokestop);
            }
        }
    }
}