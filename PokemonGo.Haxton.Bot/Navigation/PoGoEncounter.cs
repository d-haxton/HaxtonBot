using NLog;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Inventory;
using PokemonGo.Haxton.Bot.Utilities;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static POGOProtos.Networking.Responses.EncounterResponse;

namespace PokemonGo.Haxton.Bot.Navigation
{
    public interface IPoGoEncounter
    {
        Task<EncounterResponse> EncounterPokemonAsync(MapPokemon pokemon);

        Task<CatchPokemonResponse> CatchPokemon(EncounterResponse encounter, MapPokemon pokemon);

        Task<DiskEncounterResponse> EncounterPokemonLure(ulong encounterId, string spawnPointGuid);

        Task<CatchPokemonResponse> CatchPokemon(ulong encounterId, string id, DiskEncounterResponse encounter, PokemonId pokemonId);
    }

    public class PoGoEncounter : IPoGoEncounter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IApiEncounter _apiEncounter;
        private readonly IPoGoNavigation _navigation;
        private readonly IPoGoInventory _inventory;
        private readonly ILogicSettings _logicSettings;

        private object lockObject = new object();

        public PoGoEncounter(IApiEncounter apiEncounter, IPoGoNavigation navigation, IPoGoInventory inventory, ILogicSettings logicSettings)
        {
            _apiEncounter = apiEncounter;
            _navigation = navigation;
            _inventory = inventory;
            _logicSettings = logicSettings;
        }

        public async Task<EncounterResponse> EncounterPokemonAsync(MapPokemon pokemon)
        {
            var encounter = await _apiEncounter.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnPointId);
            return encounter;
        }

        public async Task<DiskEncounterResponse> EncounterPokemonLure(ulong encounterId, string fortId)
        {
            return await _apiEncounter.EncounterLurePokemon(encounterId, fortId);
        }

        public async Task<CatchPokemonResponse> CatchPokemon(ulong encounterId, string id, DiskEncounterResponse encounter, PokemonId pokemonId)
        {
            CatchPokemonResponse caughtPokemonResponse;
            var attempts = 0;
            do
            {
                var probability = encounter?.CaptureProbability?.CaptureProbability_?.FirstOrDefault();
                var pokeball = GetPokeball(encounter);

                caughtPokemonResponse =
                    await _apiEncounter.CatchPokemon(encounterId, id, pokeball);
                logger.Info($"[{caughtPokemonResponse.Status} - {attempts}] {pokemonId} encountered. {PokemonInfo.CalculatePokemonPerfection(encounter?.PokemonData)}% perfect. {encounter?.PokemonData?.Cp} CP. Probability: {probability}");
                attempts++;
            } while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed ||
                     caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchEscape);
            return caughtPokemonResponse;
        }

        public async Task<CatchPokemonResponse> CatchPokemon(EncounterResponse encounter, MapPokemon pokemon)
        {
            CatchPokemonResponse caughtPokemonResponse;
            var attempts = 0;
            //var r = new Random((int)DateTime.Now.Ticks);
            //var waitTime = r.Next(100, 3000);
            do
            {
                var probability = encounter?.CaptureProbability?.CaptureProbability_?.FirstOrDefault();

                var pokeball = GetPokeball(encounter);
                if (pokeball == ItemId.ItemUnknown)
                    return new CatchPokemonResponse();
                var isLowProbability = probability.HasValue && probability.Value < 0.35;
                var isHighCp = encounter != null && encounter.WildPokemon?.PokemonData?.Cp > 400;
                var isHighPerfection = PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData) >= _logicSettings.KeepMinIvPercentage;

                if ((isLowProbability && isHighCp) || isHighPerfection)
                    UseBerry(pokemon.EncounterId, pokemon.SpawnPointId);

                //var distance = LocationUtils.CalculateDistanceInMeters(_navigation.CurrentLatitude,
                //    _navigation.CurrentLongitude, pokemon.Latitude, pokemon.Longitude);

                //lock (lockObject)
                //    Thread.Sleep(waitTime);
                caughtPokemonResponse =
                    await _apiEncounter.CatchPokemon(pokemon.EncounterId, pokemon.SpawnPointId, pokeball);
                logger.Info($"[{caughtPokemonResponse.Status} - {attempts}] {pokemon.PokemonId} {Math.Round(PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData), 1)}% perfect. {encounter?.WildPokemon?.PokemonData?.Cp} CP. Probabilty: {Math.Round((double)probability * 100, 1)} with ball: {pokeball}");
                attempts++;
            } while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed ||
                     caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchEscape);
            return caughtPokemonResponse;
        }

        private async void UseBerry(ulong encounterId, string spawnPointId)
        {
            logger.Info("Using berry");
            var cachedInventory = _inventory.Items;
            var berries = cachedInventory.Where(p => p.ItemId == ItemId.ItemRazzBerry);
            var berry = berries.FirstOrDefault();

            if (berry == null || berry.Count <= 0)
                return;
            try
            {
                await _apiEncounter.UseCaptureItem(encounterId, ItemId.ItemRazzBerry, spawnPointId);
            }
            catch(InvalidOperationException ex)
            {
                logger.Error(ex.ToString());
            }
            berry.Count -= 1;

            await Task.Delay(100);
        }

        private ItemId GetPokeball(EncounterResponse encounter)
        {
            var pokemonCp = encounter?.WildPokemon?.PokemonData?.Cp;
            var iV = Math.Round(PokemonInfo.CalculatePokemonPerfection(encounter?.WildPokemon?.PokemonData));
            var proba = encounter?.CaptureProbability?.CaptureProbability_.First();

            var pokeBallsCount = _inventory.GetItemAmountByType(ItemId.ItemPokeBall);
            var greatBallsCount = _inventory.GetItemAmountByType(ItemId.ItemGreatBall);
            var ultraBallsCount = _inventory.GetItemAmountByType(ItemId.ItemUltraBall);
            var masterBallsCount = _inventory.GetItemAmountByType(ItemId.ItemMasterBall);

            if (masterBallsCount > 0 && pokemonCp >= 1200)
                return ItemId.ItemMasterBall;
            if (ultraBallsCount > 0 && pokemonCp >= 1000)
                return ItemId.ItemUltraBall;
            if (greatBallsCount > 0 && pokemonCp >= 750)
                return ItemId.ItemGreatBall;

            if (ultraBallsCount > 0 && iV >= _logicSettings.KeepMinIvPercentage && proba < 0.40)
                return ItemId.ItemUltraBall;

            if (greatBallsCount > 0 && iV >= _logicSettings.KeepMinIvPercentage && proba < 0.50)
                return ItemId.ItemGreatBall;

            if (greatBallsCount > 0 && pokemonCp >= 300)
                return ItemId.ItemGreatBall;

            if (pokeBallsCount > 0)
                return ItemId.ItemPokeBall;
            if (greatBallsCount > 0)
                return ItemId.ItemGreatBall;
            if (ultraBallsCount > 0)
                return ItemId.ItemUltraBall;
            if (masterBallsCount > 0)
                return ItemId.ItemMasterBall;

            return ItemId.ItemUnknown;
        }

        private ItemId GetPokeball(DiskEncounterResponse encounter)
        {
            var pokemonCp = encounter?.PokemonData?.Cp;
            var iV = Math.Round(PokemonInfo.CalculatePokemonPerfection(encounter?.PokemonData));
            var proba = encounter?.CaptureProbability?.CaptureProbability_.First();

            var pokeBallsCount = _inventory.GetItemAmountByType(ItemId.ItemPokeBall);
            var greatBallsCount = _inventory.GetItemAmountByType(ItemId.ItemGreatBall);
            var ultraBallsCount = _inventory.GetItemAmountByType(ItemId.ItemUltraBall);
            var masterBallsCount = _inventory.GetItemAmountByType(ItemId.ItemMasterBall);

            if (masterBallsCount > 0 && pokemonCp >= 1200)
                return ItemId.ItemMasterBall;
            if (ultraBallsCount > 0 && pokemonCp >= 1000)
                return ItemId.ItemUltraBall;
            if (greatBallsCount > 0 && pokemonCp >= 750)
                return ItemId.ItemGreatBall;

            if (ultraBallsCount > 0 && iV >= _logicSettings.KeepMinIvPercentage && proba < 0.40)
                return ItemId.ItemUltraBall;

            if (greatBallsCount > 0 && iV >= _logicSettings.KeepMinIvPercentage && proba < 0.50)
                return ItemId.ItemGreatBall;

            if (greatBallsCount > 0 && pokemonCp >= 300)
                return ItemId.ItemGreatBall;

            if (pokeBallsCount > 0)
                return ItemId.ItemPokeBall;
            if (greatBallsCount > 0)
                return ItemId.ItemGreatBall;
            if (ultraBallsCount > 0)
                return ItemId.ItemUltraBall;
            if (masterBallsCount > 0)
                return ItemId.ItemMasterBall;

            return ItemId.ItemUnknown;
        }
    }
}
