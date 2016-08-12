using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MahApps.Metro.Controls.Dialogs;
using PokemonGo.Haxton.Bot.UI.Model;
using PokemonGo.Haxton.Bot.UI.Provider;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PokemonGo.Haxton.Bot.UI.ViewModel
{
    [ImplementPropertyChanged]
    public class MainViewModel : ViewModelBase
    {
        private readonly IAccountProvider _accountProvider;
        public RelayCommand AddAccountCommand { get; }
        public ObservableCollection<AccountModel> Accounts => _accountProvider.Accounts;
        private readonly DialogCoordinator _dialogCoordinator;

        public AccountModel SelectedAccount
        {
            get { return _accountProvider.SelectedAccount; }
            set { _accountProvider.SelectedAccount = value; }
        }

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }

        public MainViewModel(IAccountProvider accountProvider)
        {
            _dialogCoordinator = DialogCoordinator.Instance;
            _accountProvider = accountProvider;
            AddAccountCommand = new RelayCommand(OnAddAccountCommand);

            StartCommand = new RelayCommand(OnStartCommand);
            StopCommand = new RelayCommand(OnStopCommand);
        }

        private void OnStopCommand()
        {
        }

        private void OnStartCommand()
        {
        }

        private async void OnAddAccountCommand()
        {
            var input =
                await
                    _dialogCoordinator.ShowInputAsync(this, "New Account Nickname",
                        "Please enter a nickname easy for you to remember about this account.");
            _accountProvider.Accounts.Add(new AccountModel(input));
        }
    }
}