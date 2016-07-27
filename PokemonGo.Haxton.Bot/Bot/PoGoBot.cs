using MoreLinq;
using NLog;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using PokemonGo.Haxton.Bot.Inventory;
using PokemonGo.Haxton.Bot.Navigation;
using PokemonGo.Haxton.Bot.Utilities;
using PokemonGo.RocketAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Bot
{
    public interface IPoGoBot
    {
        bool ShouldRecycleItems { get; set; }
        bool ShouldEvolvePokemon { get; set; }
        bool ShouldTransferPokemon { get; set; }

        List<Task> Run();
    }

    public class PoGoBot : IPoGoBot
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private DateTime LuckyEggUsed { get; set; }
        private readonly IPoGoNavigation _navigation;
        private readonly IPoGoInventory _inventory;
        private readonly IPoGoEncounter _encounter;
        private readonly IPoGoFort _fort;
        private readonly IPoGoMap _map;
        private readonly ILogicSettings _settings;

        public bool ShouldRecycleItems { get; set; }
        public bool ShouldEvolvePokemon { get; set; }
        public bool ShouldTransferPokemon { get; set; }

        public PoGoBot(IPoGoNavigation navigation, IPoGoInventory inventory, IPoGoEncounter encounter, IPoGoFort fort, IPoGoMap map, ILogicSettings settings)
        {
            _navigation = navigation;
            _inventory = inventory;
            _encounter = encounter;
            _fort = fort;
            _map = map;
            _settings = settings;

            LuckyEggUsed = DateTime.MinValue;

            ShouldTransferPokemon = _settings.TransferDuplicatePokemon;
            ShouldEvolvePokemon = _settings.EvolveAllPokemonWithEnoughCandy || _settings.EvolveAllPokemonAboveIv;
            ShouldRecycleItems = _settings.ItemRecycleFilter.Count > 0;
        }

        public List<Task> Run()
        {
            logger.Info("Starting bot.");

            var taskList = new List<Task>();

            taskList.Add(Task.Run(RecycleItemsTask));
            taskList.Add(Task.Run(TransferDuplicatePokemon));
            taskList.Add(Task.Run(FarmPokestopsTask));
            return taskList;
        }

        private async Task FarmPokestopsTask()
        {
            FortData firstPokestop = null;
            var numberOfPokestopsVisited = 0;
            var returnToStart = DateTime.Now;
            while (true)
            {
                if (returnToStart.AddMinutes(10) <= DateTime.Now)
                {
                    _navigation.TeleportToPokestop(firstPokestop);
                    returnToStart = DateTime.Now;
                }
                var pokestopList = (await _map.GetPokeStops()).Where(t => t.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).ToList();
                if (!pokestopList.Any())
                {
                    _navigation.TeleportToPokestop(firstPokestop);
                    pokestopList = (await _map.GetPokeStops()).Where(t => t.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).ToList();
                }
                //while (pokestopList.Any())
                //{
                logger.Info($"Found {pokestopList.Count} pokestops.");
                var closestPokestop = pokestopList.OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(_navigation.CurrentLatitude,
                            _navigation.CurrentLongitude, i.Latitude, i.Longitude)).First();
                if (firstPokestop == null)
                    firstPokestop = closestPokestop;
                //pokestopList.Remove(closestPokestop);

                //var distance = LocationUtils.CalculateDistanceInMeters(_navigation.CurrentLatitude,
                //    _navigation.CurrentLongitude, closestPokestop.Latitude, closestPokestop.Longitude);

                //var pokestop =
                //    await _fort.GetFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);

                logger.Info("Moving to a pokestop");
                //await
                //    _navigation.HumanLikeWalking(
                //        new GeoCoordinate(closestPokestop.Latitude, closestPokestop.Longitude),
                //        _settings.WalkingSpeedInKilometerPerHour, CatchNearbyPokemon);

                _navigation.TeleportToPokestop(closestPokestop);
                var pokestopBooty = await _fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
                if (pokestopBooty.ExperienceAwarded > 0)
                {
                    logger.Info(
                        $"[{numberOfPokestopsVisited++}] Pokestop rewarded us with {pokestopBooty.ExperienceAwarded} exp. {pokestopBooty.GemsAwarded} gems. {StringUtils.GetSummedFriendlyNameOfItemAwardList(pokestopBooty.ItemsAwarded)}.");
                    //_stats.ExperienceSinceStarted += pokestopBooty.ExperienceAwarded;
                    //_stats.
                }
                else
                {
                    while (pokestopBooty.Result == FortSearchResponse.Types.Result.Success)
                    {
                        pokestopBooty =
                            await
                                _fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude,
                                    closestPokestop.Longitude);
                    }
                }
                await CatchNearbyPokemon(closestPokestop);

                //await Task.Delay(100);
                //}
            }
        }

        private async Task CatchNearbyPokemon(FortData fortData)
        {
            var pokemon = _map.GetNearbyPokemonClosestFirst().GetAwaiter().GetResult().DistinctBy(i => i.SpawnPointId).ToList();
            if (pokemon.Any())
            {
                var pokemonList = string.Join(", ", pokemon.Select(x => x.PokemonId).ToArray());
                logger.Info($"{pokemon.Count()} Pokemon found: {pokemonList}");
            }
            if (fortData?.LureInfo != null && fortData.LureInfo.ActivePokemonId != PokemonId.Missingno)
            {
                var encounterId = fortData.LureInfo.EncounterId;
                var encounter = await _encounter.EncounterPokemonLure(encounterId, fortData.Id);
                if (encounter.Result == DiskEncounterResponse.Types.Result.Success)
                {
                    await _encounter.CatchPokemon(encounterId, fortData.Id, encounter, encounter.PokemonData.PokemonId);
                }
            }
            foreach (var mapPokemon in pokemon)
            {
                //logger.Info($"Found {pokemon.Count()} pokemon in your area.");
                if (_settings.UsePokemonToNotCatchFilter &&
                    _settings.PokemonsNotToCatch.Contains(mapPokemon.PokemonId))
                {
                    continue;
                }

                var encounter = await _encounter.EncounterPokemonAsync(mapPokemon);
                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    await _encounter.CatchPokemon(encounter, mapPokemon);
                }
                else
                {
                    if (encounter.Status != EncounterResponse.Types.Status.EncounterAlreadyHappened)
                        logger.Warn($"Unable to catch pokemon. Reason: {encounter.Status}");
                }
            }
        }

        private async Task TransferDuplicatePokemon()
        {
            while (ShouldTransferPokemon)
            {
                EvolvePokemonTask();
                var duplicatePokemon = _inventory.GetDuplicatePokemonForTransfer(_settings.KeepPokemonsThatCanEvolve, _settings.PrioritizeIvOverCp, _settings.PokemonsNotToTransfer);
                foreach (var pokemonData in duplicatePokemon)
                {
                    if (pokemonData.Cp >= _settings.KeepMinCp || PokemonInfo.CalculatePokemonPerfection(pokemonData) > _settings.KeepMinIvPercentage)
                    {
                        continue;
                    }
                    logger.Info($"Transferring pokemon {pokemonData.PokemonId} with cp {pokemonData.Cp}.");
                    await _inventory.TransferPokemon(pokemonData.Id);

                    //var bestPokemon = _settings.PrioritizeIvOverCp
                    //    ? _inventory.GetBestPokemonByIv(pokemonData.PokemonId)
                    //    : _inventory.GetBestPokemonByCp(pokemonData.PokemonId)
                    //    ?? pokemonData;
                }
                await Task.Delay(30000);
            }
        }

        private void EvolvePokemonTask()
        {
            if (_settings.UseLuckyEggsWhileEvolving)
            {
                logger.Info("Using lucky egg.");
                LuckyEgg();
            }
            var list = _settings.PokemonsToEvolve;
            if (_settings.EvolveAllPokemonWithEnoughCandy)
            {
                list = null;
            }
            var pokemon = _inventory.GetPokemonToEvolve(list).ToList();
            pokemon.ForEach(async p =>
            {
                logger.Info($"Evolving pokemon {p.PokemonId} with cp {p.Cp}.");
                await _inventory.EvolvePokemon(p.Id);
            });
        }

        private async void LuckyEgg()
        {
            if (LuckyEggUsed.AddMinutes(30) < DateTime.Now)
            {
                var inventoryContent = _inventory.Items;

                var luckyEggs = inventoryContent.Where(p => p.ItemId == ItemId.ItemLuckyEgg);
                var luckyEgg = luckyEggs.FirstOrDefault();

                if (luckyEgg == null || luckyEgg.Count <= 0)
                    return;
                logger.Info($"Lucky egg used. {luckyEgg.Count} remaining");
                await _inventory.UseLuckyEgg();
                await Task.Delay(2000);
            }
            logger.Info("Lucky egg not used. Still have one in effect.");
        }

        private async Task RecycleItemsTask()
        {
            while (ShouldRecycleItems)
            {
                var itemsToThrowAway = _inventory.GetItemsToRecycle(_settings.ItemRecycleFilter).ToList();
                itemsToThrowAway.ForEach(async x =>
                {
                    logger.Info($"Recycling item(s): {x.ItemId} x{x.Count}");
                    _inventory.RecycleItems(x.ItemId, x.Count);
                    await Task.Delay(500);
                });
                await Task.Delay(30000);
            }
        }
    }
}