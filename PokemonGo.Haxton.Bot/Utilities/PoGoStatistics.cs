using POGOProtos.Data.Player;
using POGOProtos.Networking.Responses;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Inventory;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable CyclomaticComplexity

namespace PokemonGo.Haxton.Bot.Utilities
{
    public interface IPoGoStatistics
    {
        string statistics();
    }

    public class PoGoStatistics : IPoGoStatistics
    {
        private readonly IPoGoInventory _inventory;
        private readonly IApiPlayer _player;
        private DateTime _initSessionDateTime;
        private PlayerStats _initPlayerStats;
        private PlayerStats _currentPlayerStats;
        private String _playerName = null;
        private int _totalStardust;

        public PoGoStatistics(IPoGoInventory inventory, IApiPlayer player)
        {
            _inventory = inventory;
            _player = player;
            _initSessionDateTime = DateTime.Now;
            Task.Run(getInitialStats);
            Task.Run(UpdateStats);
        }

        private async Task getInitialStats()
        {
            do
            {
                _initPlayerStats = _inventory.PlayerStats.FirstOrDefault();
                _currentPlayerStats = _initPlayerStats;
                await Task.Delay(1000);
            }
            while (_initPlayerStats == null);
        }

        private async Task UpdateStats()
        {
            while (true)
            {
                var player = (await _player.GetPlayer()).PlayerData;
                if (_playerName == null)
                {
                    _playerName = player?.Username;
                }
                _totalStardust = player?.Currencies.FirstOrDefault(t => t.Name == "STARDUST")?.Amount ?? 0;
                _currentPlayerStats = _inventory.PlayerStats.FirstOrDefault();
                await Task.Delay(15000);
            }
        }

        public string statistics()
        {
            if (_initPlayerStats == null)
            {
                return "Statistics not ready yet";
            }
            long experienceSinceStarted = _currentPlayerStats.Experience - _initPlayerStats.Experience;
            // TNL - to next level
            long currentExperienceTNL = _currentPlayerStats.Experience - _currentPlayerStats.PrevLevelXp - experienceToLevel(_currentPlayerStats.Level);
            long totalExperienceTNL = _currentPlayerStats.NextLevelXp - _currentPlayerStats.PrevLevelXp - experienceToLevel(_currentPlayerStats.Level);
            long remainingExperienceTNL = totalExperienceTNL - currentExperienceTNL;
            double experiencePerHour = experienceSinceStarted / (runtime().TotalSeconds / 3600);
            double timeTNL = Math.Round(remainingExperienceTNL / experiencePerHour, 2);
            double hoursTNL = 0.00;
            double minutesTNL = 0.00;
            if (double.IsInfinity(timeTNL) == false && timeTNL > 0)
            {
                timeTNL = Convert.ToDouble(TimeSpan.FromHours(timeTNL).ToString("h\\.mm"), CultureInfo.InvariantCulture);
                hoursTNL = Math.Truncate(timeTNL);
                minutesTNL = Math.Round((timeTNL - hoursTNL) * 100);
            }
            int pokemonsCaptured = _currentPlayerStats.PokemonsCaptured - _initPlayerStats.PokemonsCaptured;
            // this is not working
            //int pokemonsTransfered = _currentPlayerStats.PokemonDeployed - _initPlayerStats.PokemonDeployed;
            return $"{_playerName} (next level in {hoursTNL}h {minutesTNL}m |"
                + $" {currentExperienceTNL}/{totalExperienceTNL} XP) - Runtime {runtime().ToString(@"dd\.hh\:mm\:ss")}"
                + $" - Lvl: {_currentPlayerStats.Level} | EXP/H: {experiencePerHour:0} | P/H: {(pokemonsCaptured / (runtime().TotalSeconds / 3600)):0}"
                + $" | Stardust: {_totalStardust:0}";
        }

        private TimeSpan runtime()
        {
            return (DateTime.Now - _initSessionDateTime);
        }

        private long experienceToLevel(int level)
        {
            if (level > 0 && level <= 40)
            {
                int[] xpTable = { 0, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
                    10000, 10000, 10000, 10000, 15000, 20000, 20000, 20000, 25000, 25000,
                    50000, 75000, 100000, 125000, 150000, 190000, 200000, 250000, 300000, 350000,
                    500000, 500000, 750000, 1000000, 1250000, 1500000, 2000000, 2500000, 3000000, 5000000};
                return xpTable[level - 1];
            }
            return 0;
        }
    }
}