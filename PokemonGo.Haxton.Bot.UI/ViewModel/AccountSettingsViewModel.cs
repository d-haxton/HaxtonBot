using GalaSoft.MvvmLight;
using PokemonGo.Haxton.Bot.UI.Model;
using PokemonGo.Haxton.Bot.UI.Provider;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.ViewModel
{
    [ImplementPropertyChanged]
    public class AccountSettingsViewModel : ViewModelBase
    {
        private readonly IAccountProvider _accountProvider;
        public AccountModel Account => _accountProvider.SelectedAccount;
        public IEnumerable<EAccountType> AccountTypes => Enum.GetValues(typeof(EAccountType)).Cast<EAccountType>();

        public AccountSettingsViewModel(IAccountProvider accountProvider)
        {
            _accountProvider = accountProvider;
        }
    }
}