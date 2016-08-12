using MoreLinq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using POGOProtos.Enums;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Navigation;
using PokemonGo.Haxton.Bot.Utilities;
using PokemonGo.Haxton.PoGoBot.Model;
using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace PokemonGo.Haxton.Bot.Bot
{
    public interface IPoGoPokemon
    {
        Task<IEnumerable<MapPokemon>> GetPokemon();

        Task EncounterPokemonAndCatch(IEnumerable<MapPokemon> pokemon);

        Task<Func<bool>> EncounterLurePokemon(FortData pokestop);

        Task EncounterLurePokemonAndCatch(FortData pokestop);

        Task<IEnumerable<Action>> EncounterPokemon(IEnumerable<MapPokemon> pokemon);

        Task<IEnumerable<MapPokemon>> PokemonToCatch(IEnumerable<MapPokemon> foundPokemon);

        IEnumerable<MapPokemon> CloudPokemon(int count);
    }

    public class PoGoPokemon : IPoGoPokemon
    {
        private readonly IPoGoNavigation _navigation;
        private readonly IPoGoEncounter _encounter;
        private readonly IApiMap _map;
        private readonly ILogicSettings _logicSettings;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PoGoPokemon(IPoGoNavigation navigation, IPoGoEncounter encounter, IApiMap map, ILogicSettings logicSettings)
        {
            _navigation = navigation;
            _encounter = encounter;
            _map = map;
            _logicSettings = logicSettings;
        }

        private RestClient Rc { get; } = new RestClient("http://haxton.io/");
        private readonly List<string> _foundPokemonAlready = new List<string>();

        public IEnumerable<MapPokemon> CloudPokemon(int take)
        {
            var cloudPokemon = new List<MapPokemon>();

            var request = new RestRequest($"api/pokemon", Method.GET);
            var response = Rc.Execute(request);
            if (response.Content.Length > 2)
            {
                dynamic json = JObject.Parse("{\"pokemon\":" + response.Content + "}");
                foreach (var pokemonString in json.pokemon)
                {
                    string encounter = pokemonString.EncounterId;
                    string expires = pokemonString.Expires;
                    string latitude = pokemonString.Latitude;
                    string longitude = pokemonString.Longitude;
                    string pokemonid = pokemonString.Name;
                    string spawn = pokemonString.SpawnId;
                    var mapPokemon = new MapPokemon()
                    {
                        EncounterId = ulong.Parse(encounter, CultureInfo.InvariantCulture),
                        ExpirationTimestampMs = long.Parse(expires, CultureInfo.InvariantCulture),
                        Latitude = double.Parse(latitude, CultureInfo.InvariantCulture),
                        Longitude = double.Parse(longitude, CultureInfo.InvariantCulture),
                        PokemonId = EnumHelper.ParseEnum<PokemonId>(pokemonid),
                        SpawnPointId = spawn
                    };
                    if (!_foundPokemonAlready.Contains(encounter + spawn))
                    {
                        cloudPokemon.Add(mapPokemon);
                    }
                }
            }
            var catchableCloud = cloudPokemon.Take(take).ToList();
            _foundPokemonAlready.AddRange(catchableCloud.Select(t => t.EncounterId + t.SpawnPointId));

            return catchableCloud;
        }

        public async Task<IEnumerable<MapPokemon>> GetPokemon()
        {
            var mapObjects = await _map.GetMapObjects();
            var catchable = mapObjects.Item1.MapCells.SelectMany(i => i.CatchablePokemons).ToList();

            return
                catchable.OrderBy(
                    t =>
                        LocationUtils.CalculateDistanceInMeters(_navigation.CurrentLatitude,
                            _navigation.CurrentLongitude, t.Latitude, t.Longitude))
                            .DistinctBy(x => x.SpawnPointId);
        }

        public async Task EncounterPokemonAndCatch(IEnumerable<MapPokemon> pokemon)
        {
            var lat = _navigation.CurrentLatitude;
            var lng = _navigation.CurrentLongitude;
            var mapPokemons = pokemon as MapPokemon[] ?? pokemon.ToArray();
            var pokemonToRetry = mapPokemons.ToList();
            foreach (var mapPokemon in mapPokemons)
            {
                if (_logicSettings.UsePokemonToNotCatchFilter && _logicSettings.PokemonsNotToCatch.Contains(mapPokemon.PokemonId))
                {
                    continue;
                }
                await _navigation.TeleportToLocation(mapPokemon.Latitude, mapPokemon.Longitude);
                var encounter = await _encounter.EncounterPokemonAsync(mapPokemon);
                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    try
                    {
                        await _navigation.TeleportToLocation(lat, lng);
                        pokemonToRetry.Remove(mapPokemon);

                        if ((await _encounter.CatchPokemon(encounter, mapPokemon)).Status ==
                            CatchPokemonResponse.Types.CatchStatus.CatchError)
                        {
                            _map.EncounterSpawnList.Remove(mapPokemon.EncounterId + mapPokemon.SpawnPointId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Unable to catch pokemon");
                    }
                }
                else
                {
                    logger.Warn($"Encounter failed with reason : {encounter.Status}");
                }
            }
        }

        public async Task<Func<bool>> EncounterLurePokemon(FortData pokestop)
        {
            Func<bool> returnAction = () => false;
            if (pokestop?.LureInfo != null && pokestop.LureInfo.ActivePokemonId != PokemonId.Missingno)
            {
                var encounterId = pokestop.LureInfo.EncounterId;
                var encounter = await _encounter.EncounterPokemonLure(encounterId, pokestop.Id);
                if (encounter.Result == DiskEncounterResponse.Types.Result.Success)
                {
                    returnAction =
                        () => _encounter.CatchPokemon(encounterId, pokestop.Id, encounter,
                            encounter.PokemonData.PokemonId).GetAwaiter().GetResult().Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess;
                }
            }
            return returnAction;
        }

        public async Task EncounterLurePokemonAndCatch(FortData pokestop)
        {
            if (pokestop?.LureInfo != null && pokestop.LureInfo.ActivePokemonId != PokemonId.Missingno)
            {
                var encounterId = pokestop.LureInfo.EncounterId;
                var encounter = await _encounter.EncounterPokemonLure(encounterId, pokestop.Id);
                if (encounter.Result == DiskEncounterResponse.Types.Result.Success)
                {
                    await
                        _encounter.CatchPokemon(encounterId, pokestop.Id, encounter,
                            encounter.PokemonData.PokemonId);
                }
            }
        }

        public async Task<IEnumerable<Action>> EncounterPokemon(IEnumerable<MapPokemon> pokemon)
        {
            var actionList = new List<Action>();
            foreach (var mapPokemon in pokemon)
            {
                if (_logicSettings.UsePokemonToNotCatchFilter && _logicSettings.PokemonsNotToCatch.Contains(mapPokemon.PokemonId))
                {
                    continue;
                }
                var encounter = await _encounter.EncounterPokemonAsync(mapPokemon);
                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    actionList.Add(() =>
                    {
                        try
                        {
                            _encounter.CatchPokemon(encounter, mapPokemon).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Unable to catch pokemon");
                        }
                    });
                }
                else
                {
                    logger.Warn($"Encounter failed with reason : {encounter.Status}");
                }
            }
            return actionList;
        }

        public async Task<IEnumerable<MapPokemon>> PokemonToCatch(IEnumerable<MapPokemon> foundPokemon)
        {
            var pokemon = await GetPokemon();
            return _logicSettings.UsePokemonToNotCatchFilter ? pokemon.Where(x => _logicSettings.PokemonsNotToCatch.Contains(x.PokemonId)) : pokemon;
        }
    }

    public static class EnumHelper
    {
        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}