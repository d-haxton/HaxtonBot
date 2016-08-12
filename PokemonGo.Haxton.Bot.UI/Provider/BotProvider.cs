using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using PokemonGo.Haxton.Bot.UI.Model;
using System.Collections.Generic;
using System.Linq;

namespace PokemonGo.Haxton.Bot.UI.Provider
{
    public class BotProvider
    {
        private readonly IAccountProvider _accountProvider;

        public BotProvider(IAccountProvider accountProvider)
        {
            _accountProvider = accountProvider;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }

    public class BotLogicSettings : ILogicSettings
    {
        public BotLogicSettings()
        {
        }

        public BotLogicSettings(AccountModel model)
        {
            KeepMinCp = int.Parse(model.MinCp);
            KeepMinIvPercentage = float.Parse(model.MinIv);
            WalkingSpeedInKilometerPerHour = 180;
            EvolveAllPokemonWithEnoughCandy = model.EvolveAll;
            KeepPokemonsThatCanEvolve = false;
            TransferDuplicatePokemon = model.TransferDuplicate;
            DelayBetweenPokemonCatch = 0;
            UsePokemonToNotCatchFilter = model.AvoidFilter;
            KeepMinDuplicatePokemon = 1;
            PrioritizeIvOverCp = model.IvOverCp;
            MaxTravelDistanceInMeters = 0;
            UseGpxPathing = false;
            GpxFile = "";
            UseLuckyEggsWhileEvolving = model.LuckyEggs;
            EvolveAboveIvValue = 99;
            EvolveAllPokemonAboveIv = false;
            //ItemRecycleFilter = model.
            PokemonsToEvolve = (ICollection<PokemonId>)model.EvolveList.Where(t => t.Tracking).Select(x => x.Pokemon);
            PokemonsNotToTransfer = (ICollection<PokemonId>)model.KeepList.Where(t => t.Tracking).Select(x => x.Pokemon);
            PokemonsNotToCatch = (ICollection<PokemonId>)model.AvoidList.Where(t => t.Tracking).Select(x => x.Pokemon);
            Teleport = true;
            BurstMode = true;
            LocationsToVisit = new List<KeyValuePair<double, double>>();

            ItemRecycleFilter = new Dictionary<ItemId, int>()
            {
                { ItemId.ItemUnknown, 0},
                { ItemId.ItemPokeBall, 50},
                { ItemId.ItemGreatBall, 50},
                { ItemId.ItemUltraBall ,100},
                { ItemId.ItemMasterBall ,100},
                { ItemId.ItemPotion ,0},
                { ItemId.ItemSuperPotion, 5},
                { ItemId.ItemHyperPotion ,10},
                { ItemId.ItemMaxPotion, 10},
                { ItemId.ItemRevive ,5},
                { ItemId.ItemMaxRevive, 10},
                { ItemId.ItemLuckyEgg, 200},
                { ItemId.ItemIncenseOrdinary, 100},
                { ItemId.ItemIncenseSpicy, 100},
                { ItemId.ItemIncenseCool, 100},
                { ItemId.ItemIncenseFloral, 100},
                { ItemId.ItemTroyDisk, 100},
                { ItemId.ItemXAttack, 100},
                { ItemId.ItemXDefense, 100},
                { ItemId.ItemXMiracle, 100},
                { ItemId.ItemRazzBerry ,50},
                { ItemId.ItemBlukBerry ,10},
                { ItemId.ItemNanabBerry ,10},
                { ItemId.ItemWeparBerry ,30},
                { ItemId.ItemPinapBerry, 30},
                { ItemId.ItemSpecialCamera, 100},
                { ItemId.ItemIncubatorBasicUnlimited, 1},
                { ItemId.ItemIncubatorBasic, 100},
                { ItemId.ItemPokemonStorageUpgrade, 100},
                { ItemId.ItemItemStorageUpgrade, 100}
            };
        }

        public float KeepMinIvPercentage { get; }
        public int KeepMinCp { get; }
        public double WalkingSpeedInKilometerPerHour { get; }
        public bool EvolveAllPokemonWithEnoughCandy { get; }
        public bool KeepPokemonsThatCanEvolve { get; }
        public bool TransferDuplicatePokemon { get; }
        public int DelayBetweenPokemonCatch { get; }
        public bool UsePokemonToNotCatchFilter { get; }
        public int KeepMinDuplicatePokemon { get; }
        public bool PrioritizeIvOverCp { get; }
        public int MaxTravelDistanceInMeters { get; }
        public bool UseGpxPathing { get; }
        public string GpxFile { get; }
        public bool UseLuckyEggsWhileEvolving { get; }
        public bool EvolveAllPokemonAboveIv { get; }
        public float EvolveAboveIvValue { get; }
        public Dictionary<ItemId, int> ItemRecycleFilter { get; }
        public ICollection<PokemonId> PokemonsToEvolve { get; }
        public ICollection<PokemonId> PokemonsNotToTransfer { get; }
        public ICollection<PokemonId> PokemonsNotToCatch { get; }
        public bool Teleport { get; set; }
        public bool BurstMode { get; }
        public IEnumerable<KeyValuePair<double, double>> LocationsToVisit { get; }
    }
}