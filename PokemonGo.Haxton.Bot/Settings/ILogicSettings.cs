#region using directives

using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using System.Collections.Generic;

#endregion using directives

namespace PokemonGo.Haxton.Bot
{
    public interface ILogicSettings
    {
        float KeepMinIvPercentage { get; }
        int KeepMinCp { get; }
        double WalkingSpeedInKilometerPerHour { get; }
        bool EvolveAllPokemonWithEnoughCandy { get; }
        bool KeepPokemonsThatCanEvolve { get; }
        bool TransferDuplicatePokemon { get; }
        int DelayBetweenPokemonCatch { get; }
        bool UsePokemonToNotCatchFilter { get; }
        int KeepMinDuplicatePokemon { get; }
        bool PrioritizeIvOverCp { get; }
        int MaxTravelDistanceInMeters { get; }
        bool UseGpxPathing { get; }
        string GpxFile { get; }
        bool UseLuckyEggsWhileEvolving { get; }
        bool EvolveAllPokemonAboveIv { get; }
        float EvolveAboveIvValue { get; }

		Dictionary<ItemId, int> ItemRecycleFilter { get; }

        ICollection<PokemonId> PokemonsToEvolve { get; }

        ICollection<PokemonId> PokemonsNotToTransfer { get; }

        ICollection<PokemonId> PokemonsNotToCatch { get; }

		bool Teleport { get; set; }
        bool BurstMode { get; }
        IEnumerable<KeyValuePair<double, double>> LocationsToVisit { get; }
    }
}