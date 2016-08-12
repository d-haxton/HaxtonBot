using PokemonGo.Haxton.Bot.UI.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.UI.Provider
{
    public interface IAccountProvider
    {
        ObservableCollection<AccountModel> Accounts { get; set; }
        AccountModel SelectedAccount { get; set; }
    }

    public class AccountProvider : IAccountProvider
    {
        public ObservableCollection<AccountModel> Accounts { get; set; }
        public AccountModel SelectedAccount { get; set; }

        public AccountProvider()
        {
            Accounts = new ObservableCollection<AccountModel>();
            SelectedAccount = new AccountModel("Default");
        }
    }
}