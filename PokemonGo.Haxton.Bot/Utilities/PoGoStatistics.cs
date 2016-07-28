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
        void UpdateStats();

        string GetCurrentInfo();
    }

    public class PoGoStatistics : IPoGoStatistics
    {
        private readonly IPoGoInventory _inventory;
        private readonly IApiPlayer _player;
        private DateTime _initSessionDateTime = DateTime.Now;

        private string _playerName;
        private int _currentLevelInfos;
        private PlayerStats LoginStats { get; set; }

        public long ExperienceSinceStarted { get; set; }
        public int ItemsRecycledSinceStarted { get; set; }
        public int PokemonCaughtSinceStarted { get; set; }
        public int PokemonTransferedSinceStarted { get; set; }
        public int TotalStardust { get; set; }

        public PoGoStatistics(IPoGoInventory inventory, IApiPlayer player)
        {
            _inventory = inventory;
            _player = player;
            Task.Run(UpdateStardust);
        }

        private async Task UpdateStardust()
        {
            while (true)
            {
                var player = (await _player.GetPlayer()).PlayerData;
                _playerName = player?.Username;
                TotalStardust = player?.Currencies.FirstOrDefault(t => t.Name == "STARDUST")?.Amount ?? 0;
                await Task.Delay(15000);
            }
        }

        public void UpdateStats()
        {
            GetCurrentInfo();
        }

        private string FormatRuntime()
        {
            return (DateTime.Now - _initSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }

        public string GetCurrentInfo()
        {
            var stats = _inventory.PlayerStats;
            var output = string.Empty;
            PlayerStats stat = stats.FirstOrDefault();
            if (stat != null)
            {
                if (LoginStats == null)
                {
                    _initSessionDateTime = DateTime.Now;
                    LoginStats = stat;
                }

                ExperienceSinceStarted = (stat.Experience - LoginStats.Experience);
                PokemonCaughtSinceStarted = (stat.PokemonsCaptured - LoginStats.PokemonsCaptured);
                PokemonTransferedSinceStarted = (stat.PokemonDeployed - LoginStats.PokemonDeployed);
                var ep = stat.NextLevelXp - stat.PrevLevelXp - (stat.Experience - stat.PrevLevelXp);
                var time = Math.Round(ep / (ExperienceSinceStarted / GetRuntime()), 2);
                var hours = 0.00;
                var minutes = 0.00;
                if (double.IsInfinity(time) == false && time > 0)
                {
                    time = Convert.ToDouble(TimeSpan.FromHours(time).ToString("h\\.mm"), CultureInfo.InvariantCulture);
                    hours = Math.Truncate(time);
                    minutes = Math.Round((time - hours) * 100);
                }
                _currentLevelInfos = stat.Level;
                output =
                    $"{stat.Level} (next level in {hours}h {minutes}m | {stat.Experience - stat.PrevLevelXp - GetXpDiff(stat.Level)}/{stat.NextLevelXp - stat.PrevLevelXp - GetXpDiff(stat.Level)} XP)";
            }
            return output;
        }

        public double GetRuntime()
        {
            return (DateTime.Now - _initSessionDateTime).TotalSeconds / 3600;
        }

        public static int GetXpDiff(int level)
        {
            if (level > 0 && level <= 40)
            {
                int[] xpTable = { 0, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
                    10000, 10000, 10000, 10000, 15000, 20000, 20000, 20000, 25000, 25000,
                    50000, 75000, 100000, 125000, 150000, 190000, 200000, 250000, 300000, 350000,
                    500000, 500000, 750000, 1000000, 1250000, 1500000, 2000000, 2500000, 1000000, 1000000};
                return xpTable[level - 1];
            }
            return 0;
        }

        public void SetUsername(GetPlayerResponse profile)
        {
            _playerName = profile.PlayerData.Username ?? "";
        }

        public override string ToString()
        {
            return
                $"{_playerName} - Runtime {FormatRuntime()} - Lvl: {_currentLevelInfos} | EXP/H: {ExperienceSinceStarted / GetRuntime():0} | P/H: {PokemonCaughtSinceStarted / GetRuntime():0} | Stardust: {TotalStardust:0} | Transfered: {PokemonTransferedSinceStarted:0} | Items Recycled: {ItemsRecycledSinceStarted:0}";
        }
    }
}