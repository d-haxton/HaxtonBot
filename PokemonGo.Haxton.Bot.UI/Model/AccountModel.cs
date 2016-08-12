using POGOProtos.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.Model
{
    public enum EAccountType
    {
        Ptc,
        Google
    }

    public class AccountModel
    {
        public AccountModel(string nickname)
        {
            Nickname = nickname;
            KeepList = Enum.GetValues(typeof(PokemonId)).Cast<PokemonId>().Select(x => new PokemonListModel(x));
            Inventory = new List<InventoryModel>();
            EvolveList = Enum.GetValues(typeof(PokemonId)).Cast<PokemonId>().Select(x => new PokemonListModel(x));
            AvoidList = Enum.GetValues(typeof(PokemonId)).Cast<PokemonId>().Select(x => new PokemonListModel(x));
            Pokemon = new List<PokemonModel>();
        }

        public string Nickname { get; }
        public EAccountType AccountType { get; set; }
        public string AccountUsername { get; set; }
        public string AccountPassword { get; set; }
        public string Proxy { get; set; }
        public string MinIv { get; set; }
        public string MinCp { get; set; }
        public bool TransferDuplicate { get; set; }
        public bool AvoidFilter { get; set; }
        public bool KeepFilter { get; set; }
        public bool EvolveFilter { get; set; }
        public bool IvOverCp { get; set; }
        public bool LuckyEggs { get; set; }
        public bool EvolveAll { get; set; }
        public IEnumerable<InventoryModel> Inventory { get; set; }
        public IEnumerable<PokemonListModel> KeepList { get; set; }
        public IEnumerable<PokemonListModel> EvolveList { get; set; }
        public IEnumerable<PokemonListModel> AvoidList { get; set; }
        public IEnumerable<PokemonModel> Pokemon { get; set; }
    }
}