using Google.Protobuf;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Login;
using System;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public delegate void GoogleDeviceCodeDelegate(string code, string uri);

    public interface IApiLogin
    {
        event GoogleDeviceCodeDelegate GoogleDeviceCodeEvent;

        Task DoGoogleLogin(string username, string password);

        Task DoPtcLogin(string username, string password);
    }

    public class ApiLogin : IApiLogin
    {
        private readonly IApiBaseRpc _apiBaseRpc;
        private readonly IApiClient _client;

        public ApiLogin(IApiBaseRpc apiBaseRpc, IApiClient client)
        {
            _apiBaseRpc = apiBaseRpc;
            _client = client;
        }

        public event GoogleDeviceCodeDelegate GoogleDeviceCodeEvent = delegate { };

        public async Task DoGoogleLogin(string username, string password)
        {
            _client.AuthType = AuthType.Google;

            _client.AuthToken = GoogleLogin.DoLogin(username, password);
            //GoogleLogin.TokenResponseModel tokenResponse;
            //if (_client.Settings.GoogleRefreshToken != string.Empty)
            //{
            //    tokenResponse = await GoogleLogin.GetAccessToken(_client.Settings.GoogleRefreshToken);
            //    _client.AuthToken = tokenResponse?.id_token;
            //}

            //if (_client.AuthToken == null)
            //{
            //    var deviceCode = await GoogleLogin.GetDeviceCode();
            //    GoogleDeviceCodeEvent?.Invoke(deviceCode.user_code, deviceCode.verification_url);
            //    tokenResponse = await GoogleLogin.GetAccessToken(deviceCode);
            //    _client.Settings.GoogleRefreshToken = tokenResponse?.refresh_token;
            //    _client.AuthToken = tokenResponse?.id_token;
            //}

            await SetServer();
        }

        public async Task DoPtcLogin(string username, string password)
        {
            _client.AuthToken = await PtcLogin.GetAccessToken(username, password);
            _client.AuthType = AuthType.Ptc;

            await SetServer();
        }

        private async Task SetServer()
        {
            {
                var getPlayerMessage = new GetPlayerMessage();
                var getHatchedEggsMessage = new GetHatchedEggsMessage();
                var getInventoryMessage = new GetInventoryMessage
                {
                    LastTimestampMs = DateTime.UtcNow.ToUnixTime()
                };
                var checkAwardedBadgesMessage = new CheckAwardedBadgesMessage();
                var downloadSettingsMessage = new DownloadSettingsMessage
                {
                    //05daf51635c82611d1aac95c0b051d3ec088a930
                    Hash = "05daf51635c82611d1aac95c0b051d3ec088a930"
                };

                var serverRequest = _apiBaseRpc.RequestBuilder.GetRequestEnvelope(
                    new Request
                    {
                        RequestType = RequestType.GetPlayer,
                        RequestMessage = getPlayerMessage.ToByteString()
                    }, new Request
                    {
                        RequestType = RequestType.GetHatchedEggs,
                        RequestMessage = getHatchedEggsMessage.ToByteString()
                    }, new Request
                    {
                        RequestType = RequestType.GetInventory,
                        RequestMessage = getInventoryMessage.ToByteString()
                    }, new Request
                    {
                        RequestType = RequestType.CheckAwardedBadges,
                        RequestMessage = checkAwardedBadgesMessage.ToByteString()
                    }, new Request
                    {
                        RequestType = RequestType.DownloadSettings,
                        RequestMessage = downloadSettingsMessage.ToByteString()
                    },
                    new Request()
                    {
                        RequestType = RequestType.DownloadItemTemplates
                    });

                var serverResponse = await _apiBaseRpc.PostProto<Request>(Resources.RpcUrl, serverRequest);

                if (serverResponse.AuthTicket == null)
                    throw new AccessTokenExpiredException();

                _client.AuthTicket = serverResponse.AuthTicket;
                _client.ApiUrl = serverResponse.ApiUrl;
            }
        }
    }
}