using NLog;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using PokemonGo.Haxton.Bot.Inventory;
using PokemonGo.Haxton.Bot.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Bot
{
    public interface IPoGoAsh
    {
        Task CatchEmAll();

        Task CatchEmAll(FortData pokestop);

        Task<IEnumerable<Action>> BurstCatch(double currentX, double currentY, double modifierX, double modifierY);
    }

    public class PoGoAsh : IPoGoAsh
    {
        private readonly ILogicSettings _logicSettings;
        private readonly IPoGoNavigation _navigation;
        private readonly IPoGoPokemon _pokemon;
        private readonly IPoGoInventory _inventory;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PoGoAsh(ILogicSettings logicSettings, IPoGoNavigation navigation, IPoGoPokemon pokemon, IPoGoInventory inventory)
        {
            _logicSettings = logicSettings;
            _navigation = navigation;
            _pokemon = pokemon;
            _inventory = inventory;
        }

        public async Task CatchEmAll()
        {
            var lat = _navigation.CurrentLatitude;
            var lng = _navigation.CurrentLongitude;
            var cloudPokemon = _pokemon.CloudPokemon(5).ToArray();
            logger.Warn($"{cloudPokemon.Count()} possible pokemon. Doing some magic now.");
            if (_inventory.Pokeballs > 5)
            {
                var encounters = new List<Action>();
                foreach (var mapPokemon in cloudPokemon)
                {
                    await _navigation.TeleportToLocation(mapPokemon.Latitude, mapPokemon.Longitude);
                    if (Math.Abs(_navigation.CurrentLatitude - mapPokemon.Latitude) > 0 || Math.Abs(_navigation.CurrentLongitude - mapPokemon.Longitude) > 0)
                        logger.Warn($"Did not teleport.");
                    var encounter = await _pokemon.EncounterPokemon(new List<MapPokemon> { mapPokemon });
                    encounters.AddRange(encounter);
                }
                await _navigation.TeleportToLocation(lat, lng);
                logger.Warn($"{encounters.Count} pokemon found. Catching them.");
                foreach (var encounter in encounters)
                {
                    encounter.Invoke();
                    await Task.Delay(1000);
                }
            }
            else
            {
                logger.Warn($"Low pokeballs {_inventory.Pokeballs}. Fetching some more before trying to catch.");
            }
        }

        public async Task CatchEmAll(FortData pokestop)
        {
            await CatchEmAll();
            //await _pokemon.EncounterLurePokemonAndCatch(pokestop);
        }

        public async Task<IEnumerable<Action>> BurstCatch(double currentX, double currentY, double modifierX, double modifierY)
        {
            await _navigation.TeleportToLocation(currentX + modifierX, currentY + modifierY);
            var pokemon = await _pokemon.GetPokemon();
            var encounters = await _pokemon.EncounterPokemon(pokemon);
            return encounters;
        }
    }
}