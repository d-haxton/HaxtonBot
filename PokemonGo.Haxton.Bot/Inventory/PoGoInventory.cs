using NLog;
using POGOProtos.Data;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Inventory
{
    public interface IPoGoInventory
    {
        GetInventoryResponse CachedInventory { get; set; }
        DownloadItemTemplatesResponse CachedItemTemplates { get; set; }
        IEnumerable<ItemData> Items { get; }
        DateTime LastInventoryCachcedDate { get; set; }
        IEnumerable<PlayerStats> PlayerStats { get; }
        IEnumerable<PokemonData> Pokemon { get; }
        IEnumerable<Candy> PokemonFamilies { get; }
        IEnumerable<PokemonSettings> PokemonSettings { get; }
        bool ShouldUpdateInventory { get; set; }

        void DeletePokemonFromInvById(ulong id);

        void RecycleItems(ItemId itemId, int amount);

        int GetAmountByType(ItemId type);

        PokemonData GetBestPokemonByCp(PokemonId pokemonId);

        PokemonData GetBestPokemonByIv(PokemonId pokemonId);

        IEnumerable<PokemonData> GetDuplicatePokemonForTransfer(bool keepPokemonsThatCanEvolve = false,
            bool prioritizeIvoverCp = false, IEnumerable<PokemonId> filter = null);

        IEnumerable<PokemonData> GetHighestCp(int count);

        IEnumerable<PokemonData> GetHighestPerfect(int count);

        IEnumerable<ItemData> GetItemsToRecycle(Dictionary<ItemId, int> filter);

        IEnumerable<PokemonData> GetPokemonToEvolve(IEnumerable<PokemonId> filter = null);

        Task<EvolvePokemonResponse> EvolvePokemon(ulong pokemonId);

        Task UseLuckyEgg();

        Task TransferPokemon(ulong id);

        int GetItemAmountByType(ItemId itemPokeBall);
    }

    public class PoGoInventory : IPoGoInventory
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IApiDownload _apiDownload;
        private readonly IApiInventory _apiInventory;
        private readonly ILogicSettings _logicSettings;
        private readonly CancellationToken _token;
        private GetInventoryResponse _cachedInventory;
        private DownloadItemTemplatesResponse _cachedItemTemplates;

        public PoGoInventory(IApiInventory apiInventory, IApiDownload apiDownload, ILogicSettings logicSettings, CancellationToken token)
        {
            _apiInventory = apiInventory;
            _apiDownload = apiDownload;
            _logicSettings = logicSettings;
            _token = token;

            ShouldUpdateInventory = true;

            Task.Run(UpdateInventory);
        }

        public GetInventoryResponse CachedInventory
        {
            get { return _cachedInventory?.InventoryDelta?.InventoryItems?.Any() != null ? _cachedInventory : new GetInventoryResponse() { InventoryDelta = new InventoryDelta() { InventoryItems = { new InventoryItem() { InventoryItemData = new InventoryItemData() { Item = new ItemData() } } } } }; }
            set { _cachedInventory = value; }
        }

        public DownloadItemTemplatesResponse CachedItemTemplates
        {
            get { return _cachedItemTemplates?.ItemTemplates?.Any() != null ? _cachedItemTemplates : new DownloadItemTemplatesResponse(); }
            set { _cachedItemTemplates = value; }
        }

        public IEnumerable<ItemData> Items
                    =>
                CachedInventory.InventoryDelta.InventoryItems.Select(t => t.InventoryItemData?.Item)
                    .Where(x => x != null);

        public DateTime LastInventoryCachcedDate { get; set; }

        public IEnumerable<PlayerStats> PlayerStats
            =>
                CachedInventory.InventoryDelta.InventoryItems.Select(t => t.InventoryItemData?.PlayerStats)
                    .Where(x => x != null);

        public IEnumerable<PokemonData> Pokemon
            =>
                CachedInventory.InventoryDelta.InventoryItems.Select(x => x.InventoryItemData?.PokemonData)
                    .Where(t => t?.PokemonId > 0);

        public IEnumerable<Candy> PokemonFamilies
            =>
                CachedInventory.InventoryDelta.InventoryItems.Select(x => x.InventoryItemData?.Candy)
                    .Where(t => t?.FamilyId != PokemonFamilyId.FamilyUnset);

        public IEnumerable<PokemonSettings> PokemonSettings
            =>
                CachedItemTemplates.ItemTemplates.Select(t => t.PokemonSettings)
                    .Where(p => p?.FamilyId != PokemonFamilyId.FamilyUnset);

        public bool ShouldUpdateInventory { get; set; }

        public void DeletePokemonFromInvById(ulong id)
        {
            var pokemon =
                CachedInventory.InventoryDelta.InventoryItems.FirstOrDefault(
                    i => i.InventoryItemData.PokemonData != null && i.InventoryItemData.PokemonData.Id == id);
            if (pokemon != null)
                CachedInventory.InventoryDelta.InventoryItems.Remove(pokemon);
        }

        public void RecycleItems(ItemId itemId, int amount)
        {
            RecycleInventoryItemResponse res = _apiInventory.RecycleItem(itemId, amount).GetAwaiter().GetResult();
            if(res.Result != RecycleInventoryItemResponse.Types.Result.Success)
            {
                throw new Exception("Unable to recycle item");
            }
        }

        public int GetAmountByType(ItemId type)
        {
            return Items.FirstOrDefault(t => t.ItemId == type)?.Count ?? 0;
        }

        public PokemonData GetBestPokemonByCp(PokemonId pokemonId)
        {
            return Pokemon.OrderByDescending(x => x.Cp).FirstOrDefault(t => t.PokemonId == pokemonId);
        }

        public PokemonData GetBestPokemonByIv(PokemonId pokemonId)
        {
            return Pokemon.OrderByDescending(PokemonInfo.CalculatePokemonPerfection).FirstOrDefault();
        }

        public async Task<EvolvePokemonResponse> EvolvePokemon(ulong pokemonId)
        {
            return await _apiInventory.EvolvePokemon(pokemonId);
        }

        public async Task UseLuckyEgg()
        {
            await _apiInventory.UseItemXpBoost();
        }

        public async Task TransferPokemon(ulong id)
        {
            await _apiInventory.TransferPokemon(id);
            DeletePokemonFromInvById(id);
        }

        public int GetItemAmountByType(ItemId itemPokeBall)
        {
            return Items.FirstOrDefault(i => i.ItemId == itemPokeBall)?.Count ?? 0;
        }

        public IEnumerable<PokemonData> GetDuplicatePokemonForTransfer(bool keepPokemonsThatCanEvolve = false,
            bool prioritizeIvoverCp = false, IEnumerable<PokemonId> filter = null)
        {
            // TODO:: Gut this horrible code and replace it with something that is readable
            var pokemonList =
                Pokemon.Where(
                    p => p.DeployedFortId == string.Empty && p.Favorite == 0 && p.Cp < _logicSettings.KeepMinCp)
                    .ToList();
            if (filter != null)
            {
                pokemonList = pokemonList.Where(p => !filter.Contains(p.PokemonId)).ToList();
            }
            if (keepPokemonsThatCanEvolve)
            {
                var results = new List<PokemonData>();
                var pokemonsThatCanBeTransfered = pokemonList.GroupBy(p => p.PokemonId)
                    .Where(x => x.Count() > _logicSettings.KeepMinDuplicatePokemon).ToList();

                foreach (var pokemon in pokemonsThatCanBeTransfered)
                {
                    var settings = PokemonSettings.Single(x => x.PokemonId == pokemon.Key);
                    var familyCandy = PokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);
                    var amountToSkip = _logicSettings.KeepMinDuplicatePokemon;
                    var amountPossible = familyCandy.Candy_ / settings.CandyToEvolve;

                    if (settings.CandyToEvolve > 0 && amountPossible > amountToSkip)
                        amountToSkip = amountPossible;

                    if (prioritizeIvoverCp)
                    {
                        results.AddRange(pokemonList.Where(x => x.PokemonId == pokemon.Key)
                            .OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                            .ThenBy(n => n.StaminaMax)
                            .Skip(amountToSkip)
                            .ToList());
                    }
                    else
                    {
                        results.AddRange(pokemonList.Where(x => x.PokemonId == pokemon.Key)
                            .OrderByDescending(x => x.Cp)
                            .ThenBy(n => n.StaminaMax)
                            .Skip(amountToSkip)
                            .ToList());
                    }
                }

                return results;
            }
            if (prioritizeIvoverCp)
            {
                return pokemonList
                    .GroupBy(p => p.PokemonId)
                    .Where(x => x.Any())
                    .SelectMany(
                        p =>
                            p.OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                                .ThenBy(n => n.StaminaMax)
                                .Skip(_logicSettings.KeepMinDuplicatePokemon)
                                .ToList());
            }
            return pokemonList
                .GroupBy(p => p.PokemonId)
                .Where(x => x.Any())
                .SelectMany(
                    p =>
                        p.OrderByDescending(x => x.Cp)
                            .ThenBy(n => n.StaminaMax)
                            .Skip(_logicSettings.KeepMinDuplicatePokemon)
                            .ToList());
        }

        public IEnumerable<PokemonData> GetHighestCp(int count)
        {
            return Pokemon.OrderByDescending(x => x.Cp).Take(count);
        }

        public IEnumerable<PokemonData> GetHighestPerfect(int count)
        {
            return Pokemon.OrderByDescending(PokemonInfo.CalculatePokemonPerfection).Take(count);
        }

        public IEnumerable<ItemData> GetItemsToRecycle(Dictionary<ItemId, int> filter)
        {
            return Items
                .Where(x => filter.Any(f => f.Key == x.ItemId && x.Count > f.Value))
                .Select(
                    x =>
                        new ItemData
                        {
                            ItemId = x.ItemId,
                            Count =
                                x.Count - filter.Single(f => f.Key == x.ItemId).Value,
                            Unseen = x.Unseen
                        });
        }

        public IEnumerable<PokemonData> GetPokemonToEvolve(IEnumerable<PokemonId> filter = null)
        {
            //Don't evolve pokemon in gyms
            var myPokemons = Pokemon.Where(p => p.DeployedFortId == string.Empty).OrderByDescending(p => p.Cp);
            var pokemons = filter != null ? myPokemons.Where(p => filter.Contains(p.PokemonId)).ToList() : myPokemons.ToList();

            var pokemonToEvolve = new List<PokemonData>();
            foreach (var pokemon in pokemons)
            {
                var settings = PokemonSettings.Where(t => t != null).SingleOrDefault(x => x.PokemonId == pokemon.PokemonId);
                var familyCandy = PokemonFamilies.Where(t => t != null).SingleOrDefault(x => settings?.FamilyId == x.FamilyId);

                //Don't evolve if we can't evolve it
                if (settings == null || familyCandy == null || settings.EvolutionIds.Count == 0)
                    continue;

                var pokemonCandyNeededAlready =
                    pokemonToEvolve.Count(
                        p => PokemonSettings.Where(t => t != null).Single(x => x.PokemonId == p.PokemonId).FamilyId == settings.FamilyId) *
                    settings.CandyToEvolve;

                if (_logicSettings.EvolveAllPokemonAboveIv)
                {
                    if (PokemonInfo.CalculatePokemonPerfection(pokemon) >= _logicSettings.EvolveAboveIvValue &&
                        familyCandy.Candy_ - pokemonCandyNeededAlready > settings.CandyToEvolve)
                    {
                        pokemonToEvolve.Add(pokemon);
                    }
                }
                else
                {
                    if (familyCandy.Candy_ - pokemonCandyNeededAlready > settings.CandyToEvolve)
                    {
                        pokemonToEvolve.Add(pokemon);
                    }
                }
            }

            return pokemonToEvolve;
        }

        private async Task UpdateInventory()
        {
            while (!_token.IsCancellationRequested && ShouldUpdateInventory)
            {
                var inventory = await RequestInventory();
                if (inventory.InventoryDelta?.InventoryItems != null && inventory.Success)
                    CachedInventory = inventory;
                CachedItemTemplates = await _apiDownload.GetItemTemplates();
                await Task.Delay(30000);
            }
            _token.ThrowIfCancellationRequested();
        }

        private async Task<GetInventoryResponse> RequestInventory()
        {
            await Task.Delay(1500);
            logger.Info($"Updating inventory at {DateTime.Now}");
            var inventory = await _apiInventory.GetInventory();
            return inventory;
        }
    }
}