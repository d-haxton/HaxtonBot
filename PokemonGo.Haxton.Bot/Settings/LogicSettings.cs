using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace PokemonGo.Haxton.Bot
{
    public class LogicSettings : ILogicSettings
    {
        public LogicSettings()
        {
            Teleport = Convert.ToBoolean(ConfigurationManager.AppSettings["TeleportInsteadOfWalking"]);
            KeepMinIvPercentage = Convert.ToSingle(ConfigurationManager.AppSettings["KeepMinIvPercentage"]);
            KeepMinCp = Convert.ToInt32(ConfigurationManager.AppSettings["KeepMinCp"]);
            WalkingSpeedInKilometerPerHour = Convert.ToDouble((ConfigurationManager.AppSettings["WalkingSpeedInKilometerPerHour"]));
            EvolveAllPokemonWithEnoughCandy = Convert.ToBoolean((ConfigurationManager.AppSettings["EvolveAllPokemonWithEnoughCandy"]));
            KeepPokemonsThatCanEvolve = Convert.ToBoolean((ConfigurationManager.AppSettings["KeepPokemonsThatCanEvolve"]));
            TransferDuplicatePokemon = Convert.ToBoolean(ConfigurationManager.AppSettings["TransferDuplicatePokemon"]);
            DelayBetweenPokemonCatch = Convert.ToInt32(ConfigurationManager.AppSettings["DelayBetweenPokemonCatch"]);
            UsePokemonToNotCatchFilter = Convert.ToBoolean(ConfigurationManager.AppSettings["UsePokemonToNotCatchFilter"]);
            KeepMinDuplicatePokemon = Convert.ToInt32(ConfigurationManager.AppSettings["KeepMinDuplicatePokemon"]);
            PrioritizeIvOverCp = Convert.ToBoolean(ConfigurationManager.AppSettings["PrioritizeIvOverCp"]);
            MaxTravelDistanceInMeters = Convert.ToInt32(ConfigurationManager.AppSettings["MaxTravelDistanceInMeters"]);
            UseGpxPathing = Convert.ToBoolean(ConfigurationManager.AppSettings["UseGpxPathing"]);
            GpxFile = ConfigurationManager.AppSettings["GpxFile"];
            UseLuckyEggsWhileEvolving = Convert.ToBoolean(ConfigurationManager.AppSettings["UseLuckyEggsWhileEvolving"]);
            EvolveAllPokemonAboveIv = Convert.ToBoolean(ConfigurationManager.AppSettings["EvolveAllPokemonAboveIv"]);
            EvolveAboveIvValue = Convert.ToSingle(ConfigurationManager.AppSettings["EvolveAboveIvValue"]);
            ItemRecycleFilter = GetItemRecycleFilter();
            PokemonsToEvolve = GetPokemon("UserSettings\\PokemonToEvolve.cfg");
            PokemonsNotToTransfer = GetPokemon("UserSettings\\PokemonToKeep.cfg");
            PokemonsNotToCatch = GetPokemon("UserSettings\\PokemonToAvoid.cfg");
            //
        }

        private Dictionary<ItemId, int> GetItemRecycleFilter()
        {
            var dict = new Dictionary<ItemId, int>();
            var text = File.ReadAllLines("UserSettings\\ItemListAndCount.cfg");
            foreach (var line in text)
            {
                var kvp = line.Split(' ');
                var item = (ItemId)Enum.Parse(typeof(ItemId), kvp[0]);
                var val = Convert.ToInt32(kvp[1]);
                dict.Add(item, val);
            }
            return dict;
        }

        private List<PokemonId> GetPokemon(string fileLocation)
        {
            var list = new List<PokemonId>();
            var text = File.ReadAllLines(fileLocation);
            foreach (var s in text)
            {
                var item = (PokemonId)Enum.Parse(typeof(PokemonId), s);
                list.Add(item);
            }
            return list;
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
    }
}