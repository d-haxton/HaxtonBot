using NLog;
using POGOProtos.Map.Fort;
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

        Task<IEnumerable<Func<bool>>> BurstCatch(double currentX, double currentY, double modifierX, double modifierY);
    }

    public class PoGoAsh : IPoGoAsh
    {
        private readonly ILogicSettings _logicSettings;
        private readonly IPoGoNavigation _navigation;
        private readonly IPoGoPokemon _pokemon;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PoGoAsh(ILogicSettings logicSettings, IPoGoNavigation navigation, IPoGoPokemon pokemon)
        {
            _logicSettings = logicSettings;
            _navigation = navigation;
            _pokemon = pokemon;
        }

        public async Task CatchEmAll()
        {
            var currentLat = _navigation.CurrentLatitude;
            var currentLong = _navigation.CurrentLongitude;
            var encounters = (await SearchAndEncounterPokemon()).ToList();
            //logger.Info($"We found {encounters.Count} pokemon!");
            await _navigation.TeleportToLocation(currentLat, currentLong);
            foreach (var encounter in encounters)
            {
                encounter.Invoke();
                await Task.Delay(1500);
            }
        }

        public async Task CatchEmAll(FortData pokestop)
        {
            await CatchEmAll();
            await _pokemon.EncounterLurePokemonAndCatch(pokestop);
        }

        private async Task<IEnumerable<Func<bool>>> SearchAndEncounterPokemon()
        {
            var pokemon = await _pokemon.GetPokemon();
            var basePokemon = (await _pokemon.EncounterPokemon(pokemon)).ToList();
            var actionList = basePokemon;
            if (_logicSettings.BurstMode)
            {
                var currentLat = _navigation.CurrentLatitude;
                var currentLong = _navigation.CurrentLongitude;
                await Task.Delay(1000);
                var tl = await BurstCatch(currentLat, currentLong, .001, .001);
                //await Task.Delay(1000);
                //var bl = await BurstCatch(currentLat, currentLong, .001, -.001);
                //await Task.Delay(1000);
                //var tr = await BurstCatch(currentLat, currentLong, -.001, .001);
                //await Task.Delay(1000);
                var br = await BurstCatch(currentLat, currentLong, -.001, -.001);
                actionList.AddRange(tl);
                //actionList.AddRange(bl);
                //actionList.AddRange(tr);
                actionList.AddRange(br);
            }
            return actionList;
        }

        public async Task<IEnumerable<Func<bool>>> BurstCatch(double currentX, double currentY, double modifierX, double modifierY)
        {
            await _navigation.TeleportToLocation(currentX + modifierX, currentY + modifierY);
            var pokemon = await _pokemon.GetPokemon();
            var encounters = await _pokemon.EncounterPokemon(pokemon);
            return encounters;
        }
    }
}