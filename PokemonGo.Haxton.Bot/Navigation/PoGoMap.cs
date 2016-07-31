using MoreLinq;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Utilities;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Navigation
{
    public interface IPoGoMap
    {
        Task<IOrderedEnumerable<MapPokemon>> GetNearbyPokemonClosestFirst();

        Task<IOrderedEnumerable<FortData>> GetFortWithPokemon();
    }

    public class PoGoMap : IPoGoMap
    {
        private readonly IPoGoNavigation _navigation;
        private readonly IApiMap _map;
        private readonly ISettings _settings;
        private readonly ILogicSettings _logicSettings;

        public PoGoMap(IPoGoNavigation navigation, IApiMap map, ISettings settings, ILogicSettings logicSettings)
        {
            _navigation = navigation;
            _map = map;
            _settings = settings;
            _logicSettings = logicSettings;
        }

        public async Task<IOrderedEnumerable<FortData>> GetFortWithPokemon()
        {
            var mapObjects = await _map.GetMapObjects();
            var pokeStops = mapObjects.MapCells
                .Where(t => t.CatchablePokemons.Count > 0 || t.WildPokemons.Count > 0 || t.Forts.Select(p => p.LureInfo?.ActivePokemonId != null).Any())
                .OrderByDescending(x => x.CatchablePokemons.Count)
                .SelectMany(i =>
                {
                    i.Forts.ForEach(x => x.GymPoints = i.CatchablePokemons.Count + i.WildPokemons.Count);
                    return i.Forts;
                })
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
                .OrderByDescending(x => x.GymPoints);
            return pokeStops;
        }

        public async Task<IOrderedEnumerable<MapPokemon>> GetNearbyPokemonClosestFirst()
        {
            var mapObjects = await _map.GetMapObjects();
            var catchable = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons).ToList();
            //var wild = mapObjects.MapCells.SelectMany(x => x.WildPokemons).Select(x => new MapPokemon()
            //{
            //    EncounterId = x.EncounterId,
            //    SpawnPointId = x.SpawnPointId,
            //    PokemonId = x.PokemonData.PokemonId
            //});
            //catchable.AddRange(wild);
            return
                catchable.OrderBy(t =>
                        LocationUtils.CalculateDistanceInMeters(_navigation.CurrentLatitude, _navigation.CurrentLongitude, t.Latitude, t.Longitude));
        }
    }
}