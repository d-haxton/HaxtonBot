using GalaSoft.MvvmLight;
using POGOProtos.Enums;
using PokemonGo.Haxton.Bot.Inventory;
using PokemonGo.Haxton.Bot.UI.Model;
using PokemonGo.Haxton.Bot.UI.Provider;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.ViewModel
{
    [ImplementPropertyChanged]
    public class PokemonViewModel : ViewModelBase
    {
        public ObservableCollection<PokemonModel> InventoryPokemon { get; }
        public ObservableCollection<PokemonListModel> AvoidPokemon { get; }
        public ObservableCollection<PokemonListModel> EvolvePokemon { get; }
        public ObservableCollection<PokemonListModel> KeepPokemon { get; }

        public PokemonViewModel(IAccountProvider accountProvider)
        {
            InventoryPokemon = new ObservableCollection<PokemonModel>(accountProvider.SelectedAccount.Pokemon);
            AvoidPokemon = new ObservableCollection<PokemonListModel>(accountProvider.SelectedAccount.AvoidList);
            EvolvePokemon = new ObservableCollection<PokemonListModel>(accountProvider.SelectedAccount.EvolveList);
            KeepPokemon = new ObservableCollection<PokemonListModel>(accountProvider.SelectedAccount.KeepList);
        }
    }
}