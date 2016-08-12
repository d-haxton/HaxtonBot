using GalaSoft.MvvmLight;
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
    public class InventoryViewModel : ViewModelBase
    {
        public ObservableCollection<InventoryModel> Inventory { get; }

        public InventoryViewModel(IAccountProvider accountProvider)
        {
            Inventory = new ObservableCollection<InventoryModel>(accountProvider.SelectedAccount.Inventory);
        }
    }
}