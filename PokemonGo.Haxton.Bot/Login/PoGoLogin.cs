using NLog;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.RocketAPI.Enums;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Login
{
    public interface IPoGoLogin
    {
        void DoLogin();
    }

    public class PoGoLogin : IPoGoLogin
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IApiLogin _apiLogin;
        private readonly IApiClient _client;

        public PoGoLogin(IApiLogin apiLogin, IApiClient client)
        {
            _apiLogin = apiLogin;
            _client = client;

            _apiLogin.GoogleDeviceCodeEvent += OnGoogleDeviceCode;
        }

        private async void OnGoogleDeviceCode(string code, string uri)
        {
            Process.Start(uri);
            await Task.Delay(3000);
            logger.Info($"Google device code: {code}");
        }

        public void DoLogin()
        {
            logger.Info($"Logging in with account type: {_client.AuthType}");
            if (_client.AuthType == AuthType.Google)
            {
                _apiLogin.DoGoogleLogin(_client.Settings.GoogleUsername, _client.Settings.GooglePassword).GetAwaiter().GetResult();
            }
            else
            {
                _apiLogin.DoPtcLogin(_client.Settings.PtcUsername, _client.Settings.PtcPassword).GetAwaiter().GetResult();
            }
        }
    }
}