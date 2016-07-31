using Google.Protobuf;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public interface IApiMap
    {
        Task<GetMapObjectsResponse> GetMapObjects();

        Task<GetIncensePokemonResponse> GetIncensePokemons();
    }

    public class ApiMap : IApiMap
    {
        private readonly IApiBaseRpc _baseRpc;
        private readonly IApiClient _client;

        public ApiMap(IApiBaseRpc baseRpc, IApiClient client)
        {
            _baseRpc = baseRpc;
            _client = client;
        }

        public async Task<GetMapObjectsResponse> GetMapObjects()
        {
            var getMapObjectsMessage = new GetMapObjectsMessage
            {
                CellId = { S2Helper.GetNearbyCellIds(_client.CurrentLongitude, _client.CurrentLatitude) },
                SinceTimestampMs = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                Latitude = _client.CurrentLatitude,
                Longitude = _client.CurrentLongitude
            };
            var getHatchedEggsMessage = new GetHatchedEggsMessage();
            var getInventoryMessage = new GetInventoryMessage
            {
                LastTimestampMs = DateTime.UtcNow.ToUnixTime()
            };
            var checkAwardedBadgesMessage = new CheckAwardedBadgesMessage();
            var downloadSettingsMessage = new DownloadSettingsMessage
            {
                Hash = "05daf51635c82611d1aac95c0b051d3ec088a930"
            };

            var request = _baseRpc.RequestBuilder.GetRequestEnvelope(
                new Request
                {
                    RequestType = RequestType.GetMapObjects,
                    RequestMessage = getMapObjectsMessage.ToByteString()
                },
                new Request
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
                });

            return await _baseRpc.PostProtoPayload<Request, GetMapObjectsResponse>(request);
        }

        public async Task<GetIncensePokemonResponse> GetIncensePokemons()
        {
            var message = new GetIncensePokemonMessage()
            {
                PlayerLatitude = _client.CurrentLatitude,
                PlayerLongitude = _client.CurrentLongitude
            };

            return await _baseRpc.PostProtoPayload<Request, GetIncensePokemonResponse>(RequestType.GetIncensePokemon, message);
        }
    }
}