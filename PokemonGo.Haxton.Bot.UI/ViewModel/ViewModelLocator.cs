/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:PokemonGo.Haxton.Bot.UI"
                           x:Key="Locator" />
  </Application.Resources>

  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using PokemonGo.Haxton.Bot.UI.IoC;

namespace PokemonGo.Haxton.Bot.UI.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => Bootstrapper.TheServiceLocator);
        }

        public MainViewModel MainDataContext => ServiceLocator.Current.GetInstance<MainViewModel>();
        public PokemonViewModel PokemonDataContext => ServiceLocator.Current.GetInstance<PokemonViewModel>();
        public InventoryViewModel InventoryDataContext => ServiceLocator.Current.GetInstance<InventoryViewModel>();
        public AccountSettingsViewModel AccountSettingsDataContext => ServiceLocator.Current.GetInstance<AccountSettingsViewModel>();

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}