using MoreLinq;
using NLog;
using NLog.Fluent;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
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
		private static List<string> previousFoundSnipes = new List<string>();

		private DateTime LuckyEggUsed { get; set; }
        private readonly IPoGoNavigation _navigation;
        private readonly IPoGoInventory _inventory;
        private readonly IPoGoEncounter _encounter;
        private readonly IPoGoSnipe _snipe;
        private readonly IPoGoFort _fort;
        private readonly IPoGoMap _map;
        private readonly ILogicSettings _settings;

        public bool ShouldRecycleItems { get; set; }
        public bool ShouldEvolvePokemon { get; set; }
        public bool ShouldTransferPokemon { get; set; }
		public bool isSniping { get; private set; }
		public bool isFindingAutoSnipeLocations { get; private set; }

		public PoGoBot(IPoGoNavigation navigation, IPoGoInventory inventory, IPoGoEncounter encounter, IPoGoSnipe snipe, IPoGoFort fort, IPoGoMap map, ILogicSettings settings)
        {
            _navigation = navigation;
            _inventory = inventory;
            _encounter = encounter;
            _snipe = snipe;
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

            var taskList = new List<Task>
            {
                Task
                    .Run(RecycleItemsTask),
                Task
                    .Run(TransferDuplicatePokemon),
                Task
                    .Run(FarmPokestopsTask)
            };

            return taskList;
        }

        private async Task RemoveSoftBan(FortData closestPokestop)
        {
            var pokestopBooty = await _fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
            while (pokestopBooty.Result == FortSearchResponse.Types.Result.Success)
            {
                pokestopBooty = await _fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
            }
        }

        private async Task FarmPokestopsTask()
        {
            FortData firstPokestop = null;
            var numberOfPokestopsVisited = 0;
            var returnToStart = DateTime.Now;
            var pokestopList = (await _map.GetPokeStops()).Where(t => t.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).ToList();

            while (true)
            {
                var loc = new KeyValuePair<double, double>();

				if (_settings.AutoSnipe && _snipe.SnipeLocations.Count <= 0 && !isFindingAutoSnipeLocations)
				{
					await FetchAutoSnipeLocations();
                }

                if (_snipe.SnipeLocations.Count > 0)
                {
                    if (_snipe.SnipeLocations.TryTake(out loc))
                    {
                        logger.Info($"Sniping pokemon at {loc.Key}, {loc.Value}");
                        await _navigation.TeleportToLocation(loc.Key, loc.Value);
                        isSniping = true;
                    }
                }
                else if (returnToStart.AddMinutes(2) <= DateTime.Now)
                {
                    //var r = new Random((int)DateTime.Now.Ticks);
                    //var nextLocation = _settings.LocationsToVisit.ElementAt(r.Next(_settings.LocationsToVisit.Count()));
                    //await _navigation.TeleportToLocation(nextLocation.Key, nextLocation.Value);
                    await _navigation.TeleportToPokestop(firstPokestop);
                    returnToStart = DateTime.Now;
                }

                if (!pokestopList.Any())
                {
                    await _navigation.TeleportToPokestop(firstPokestop);
                    var oldPokestopList =
                        (await _map.GetPokeStops()).Where(
                            t => t.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).ToList();
                    if (oldPokestopList.Any())
                        pokestopList = oldPokestopList;
                }
                var newPokestopList = (await _map.GetPokeStops()).Where(t => t.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).ToList();
                if (newPokestopList.Any())
                    pokestopList = newPokestopList;
                if (!pokestopList.Any())
                    continue;

                var closestPokestop = pokestopList.OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(_navigation.CurrentLatitude,
                            _navigation.CurrentLongitude, i.Latitude, i.Longitude)).First();

                if (firstPokestop == null)
                    firstPokestop = closestPokestop;

                if (_settings.Teleport)
                {
                    var distance = LocationUtils.CalculateDistanceInMeters(_navigation.CurrentLatitude, _navigation.CurrentLongitude, closestPokestop.Latitude, closestPokestop.Longitude);

                    //var fortWithPokemon = (await _map.GetFortWithPokemon());
                    //var biggestFort = fortWithPokemon.MaxBy(x => x.GymPoints);
                    if (distance > 100)
                    {
                        var r = new Random((int)DateTime.Now.Ticks);
                        closestPokestop =
                            pokestopList.ElementAt(r.Next(pokestopList.Count));
                    }
                    await _navigation.TeleportToPokestop(closestPokestop);
                }
                else
                {
                    //                var pokestop =
                    //await _fort.GetFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
                    //                await
                    await
                        _navigation.HumanLikeWalking(
                            new GeoCoordinate(closestPokestop.Latitude, closestPokestop.Longitude),
                            _settings.WalkingSpeedInKilometerPerHour, async () =>
                            {
                                await Search();
                            });
                }

                //logger.Info("Moving to a pokestop");

                var pokestopBooty =
                    await _fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
                if (pokestopBooty.ExperienceAwarded > 0)
                {
                    logger.Info(
                        $"[{numberOfPokestopsVisited++}] Pokestop rewarded us with {pokestopBooty.ExperienceAwarded} exp. {pokestopBooty.GemsAwarded} gems. {StringUtils.GetSummedFriendlyNameOfItemAwardList(pokestopBooty.ItemsAwarded)}.");
                    //_stats.ExperienceSinceStarted += pokestopBooty.ExperienceAwarded;
                    //_stats.
                }
                else
                {
                    await RemoveSoftBan(closestPokestop);
                }

                if (isSniping)
                {
                    await RemoveSoftBan(closestPokestop);
                    var x = _navigation.CurrentLatitude;
                    var y = _navigation.CurrentLongitude;
                    var burst = await CatchBurstPokemon(loc.Key, loc.Value);
                    await _navigation.TeleportToLocation(x, y);
                    burst.ForEach(a => a.Invoke());
                }
                else if (_settings.BurstMode)
                {
                    await Search();
                    var lure = await CatchLurePokemon(closestPokestop);
                    lure.Invoke();
                }
                else
                {
                    var task = (await CatchBurstPokemon(closestPokestop.Latitude, closestPokestop.Longitude)).ToArray();
                    task.ForEach(x => x.Invoke());
                }

                await Task.Delay(100);
                //}
            }
        }

        private async Task Search()
        {
            var currentLat = _navigation.CurrentLatitude;
            var currentLong = _navigation.CurrentLongitude;
            var basePokemon = (await CatchBurstPokemon(currentLat, currentLong)).ToList();
            //basePokemon.ForEach(x => x.Start());
            //Task.WaitAll(basePokemon.ToArray());

            var tl = await CatchBurstPokemon(currentLat + .002, currentLong + .002);
            var bl = await CatchBurstPokemon(currentLat - .002, currentLong + .002);
            var tr = await CatchBurstPokemon(currentLat + .002, currentLong - .002);
            var br = await CatchBurstPokemon(currentLat - .002, currentLong - .002);
            await _navigation.TeleportToLocation(currentLat, currentLong);

            var actionList = basePokemon;
            basePokemon.AddRange(tl);
            basePokemon.AddRange(bl);
            basePokemon.AddRange(tr);
            basePokemon.AddRange(br);
            foreach (var action in actionList)
            {
                action.Invoke();
                await Task.Delay(1500);
            }

            //logger.Info($"Burst mode found {actionList.Count} total pokemon. {actionList.Count - basePokemon.Count} extra from burst");

            //foreach (var act in actionList)
            //{
            //    act.Start();
            //}
            //Task.WaitAll(actionList.ToArray());
        }

        private async Task<IEnumerable<Action>> CatchBurstPokemon(double x, double y)
        {
            var actionList = new List<Action>();
            await _navigation.TeleportToLocation(x, y);
            var pokemon = (await _map.GetNearbyPokemonClosestFirst()).DistinctBy(i => i.SpawnPointId).ToList();
            foreach (var mapPokemon in pokemon)
            {
                if (_settings.UsePokemonToNotCatchFilter && _settings.PokemonsNotToCatch.Contains(mapPokemon.PokemonId))
                {
                    continue;
                }
                var encounter = await _encounter.EncounterPokemonAsync(mapPokemon);
                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    actionList.Add(async () =>
                    {
                        try
                        {
                            if (isSniping)
                                logger.Warn($"Sniping {encounter.WildPokemon.PokemonData.PokemonId}");
                            await _encounter.CatchPokemon(encounter, mapPokemon);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Unable to catch pokemon");
                        }
                    });
                }
            }
			isSniping = false;
            return actionList;
        }

        private async Task<Action> CatchLurePokemon(FortData fortData)
        {
            Action returnAction = () => { };
            if (fortData?.LureInfo != null && fortData.LureInfo.ActivePokemonId != PokemonId.Missingno)
            {
                var encounterId = fortData.LureInfo.EncounterId;
                var encounter = await _encounter.EncounterPokemonLure(encounterId, fortData.Id);
                if (encounter.Result == DiskEncounterResponse.Types.Result.Success)
                {
                    returnAction =
                        async () =>
                        {
                            await
                                _encounter.CatchPokemon(encounterId, fortData.Id, encounter,
                                    encounter.PokemonData.PokemonId);
                        };
                }
            }
            return returnAction;
        }

        //private async Task CatchNearbyPokemon(FortData fortData, bool isSniping)
        //{
        //    foreach (var mapPokemon in pokemon)
        //    {
        //        if (_settings.UsePokemonToNotCatchFilter && _settings.PokemonsNotToCatch.Contains(mapPokemon.PokemonId))
        //        {
        //            continue;
        //        }

        //        var encounter = await _encounter.EncounterPokemonAsync(mapPokemon);
        //        if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
        //        {
        //            await _encounter.CatchPokemon(encounter, mapPokemon);
        //        }
        //        else
        //        {
        //            if (encounter.Status != EncounterResponse.Types.Status.EncounterAlreadyHappened)
        //                logger.Warn($"Unable to catch pokemon. Reason: {encounter.Status}");
        //        }
        //    }
        //    if (isSniping)
        //        await _navigation.TeleportToPokestop(fortData);
        //}

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
                try
                {
                    logger.Info($"Evolving pokemon {p.PokemonId} with cp {p.Cp}.");
                    await _inventory.EvolvePokemon(p.Id);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "Failed evolving egg");
                }
            });
        }

        private async void LuckyEgg()
        {
            if (LuckyEggUsed.AddMinutes(30) < DateTime.Now)
            {
                LuckyEggUsed = DateTime.Now;
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
                    try
                    {
                        logger.Info($"Recycling item(s): {x.ItemId} x{x.Count}");
                        _inventory.RecycleItems(x.ItemId, x.Count);
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "Failed recyclying items.");
                    }
                });
                await Task.Delay(30000);
            }
        }

		private async Task FetchAutoSnipeLocations()
		{
			isFindingAutoSnipeLocations = true;
            List<SnipeLocationInfo> totalSnipeLocations = await _snipe.FetchSnipeLocations();

			if (totalSnipeLocations != null)
			{
				List<SnipeLocationInfo> filteredSnipeLocations = new List<SnipeLocationInfo>();

				logger.Info($"Total snipes found: {totalSnipeLocations.Count}");

				for (int i = 0; i < totalSnipeLocations.Count; i++)
				{
					var pokemonId = (PokemonId)Enum.Parse(typeof(PokemonId), totalSnipeLocations[i].name);
					if (!_settings.PokemonsNotToAutoSnipe.Contains(pokemonId) && !previousFoundSnipes.Any(totalSnipeLocations[i].UniqueId.Contains))
					{
						filteredSnipeLocations.Add(totalSnipeLocations[i]);
					}
				}

				logger.Info($"Filtered snipes found: {filteredSnipeLocations.Count}");
				for (int i = 0; i < filteredSnipeLocations.Count; i++)
				{
					previousFoundSnipes.Add(filteredSnipeLocations[i].UniqueId);
					string[] coords = filteredSnipeLocations[i].coords.Split(',');
					logger.Info($"Added Snipe {i + 1}: {filteredSnipeLocations[i].name} at {filteredSnipeLocations[i].coords}");
					_snipe.AddNewSnipe(coords);
				}
			} 
			else 
			{
				logger.Warn($"Failed to fetch Auto Snipe locations from http://pokesnipers.com/api/v1/pokemon.json - Retrying...");
			}
			isFindingAutoSnipeLocations = false;
        }
    }
}
