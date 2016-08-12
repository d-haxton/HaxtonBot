using Google.Protobuf;
using Newtonsoft.Json;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using PokemonGo.Haxton.Bot.Utilities;
using PokemonGo.Haxton.PoGoBot.Model;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Helpers;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public interface IApiMap
    {
        Task<Tuple<GetMapObjectsResponse, GetHatchedEggsResponse, GetInventoryResponse, CheckAwardedBadgesResponse, DownloadSettingsResponse>> GetMapObjects();

        Task<GetIncensePokemonResponse> GetIncensePokemons();

        List<string> EncounterSpawnList { get; }
    }

    public class ApiMap : IApiMap
    {
        private readonly IApiBaseRpc _baseRpc;
        private readonly IApiClient _client;

        public ApiMap(IApiBaseRpc baseRpc, IApiClient client)
        {
            _baseRpc = baseRpc;
            _client = client;

            Task.Run(SendPokemon);
            Task.Run(UpdateCanGetMap);
        }

        private async Task UpdateCanGetMap()
        {
            while (true)
            {
                CanGetMap = true;
                await Task.Delay(29000);
            }
        }

        private RestClient Rc { get; } = new RestClient("http://haxton.io/");
        private List<MapPokemon> _pokemons = new List<MapPokemon>();
        private List<FortData> _pokestops = new List<FortData>();
        public List<string> EncounterSpawnList { get; } = new List<string>();

        private async Task SendPokemon()
        {
            while (true)
            {
                var listToSend = _pokemons.Where(t => !EncounterSpawnList.Contains(t.EncounterId + t.SpawnPointId));
                var request = new RestRequest("api/pokemon", Method.POST);
                var removeList = listToSend.ToList();
                EncounterSpawnList.AddRange(removeList.Select(t => t.EncounterId + t.SpawnPointId));
                if (removeList.Count != 0)
                {
                    request.AddJsonBody(removeList.Select(x => new PokemonModel(x)));
                    //request.RequestFormat = DataFormat.Json;
                    //request.AddBody(JsonConvert.SerializeObject(removeList.Select(x => new PokemonModel(x))).Encrypt(""));
                    foreach (var mapPokemon in removeList)
                    {
                        _pokemons.Remove(mapPokemon);
                    }
                    Rc.ExecuteAsync(request, response => { });
                }
                await Task.Delay(15000);
            }
        }

        private bool CanGetMap { get; set; }

        public async Task<Tuple<GetMapObjectsResponse, GetHatchedEggsResponse, GetInventoryResponse, CheckAwardedBadgesResponse, DownloadSettingsResponse>> GetMapObjects()
        {
            while (CanGetMap == false)
            {
                await Task.Delay(150);
            }
            CanGetMap = false;
            var getMapObjectsMessage = new GetMapObjectsMessage
            {
                CellId = { S2Helper.GetNearbyCellIds(_client.CurrentLongitude, _client.CurrentLatitude) },
                SinceTimestampMs = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
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

            var response = await _baseRpc.PostProtoPayload<Request, GetMapObjectsResponse, GetHatchedEggsResponse, GetInventoryResponse, CheckAwardedBadgesResponse, DownloadSettingsResponse>(request);
            _pokemons.AddRange(response.Item1.MapCells.SelectMany(x => x.CatchablePokemons));

            //var pokestops = response.Item1.MapCells.SelectMany(t => t.Forts).Where(x => x.Type == FortType.Checkpoint);
            //_pokestops.AddRange(pokestops);
            //var newMapObjects = response.MapCells.SelectMany(x => x.WildPokemons).Select(t => new MapPokemon()
            //{
            //    EncounterId = t.EncounterId,
            //    ExpirationTimestampMs = t.TimeTillHiddenMs,
            //    Latitude = t.Latitude,
            //    Longitude = t.Longitude,
            //    PokemonId = t.PokemonData.PokemonId,
            //    SpawnPointId = t.SpawnPointId
            //});
            //_pokemons.AddRange(newMapObjects);
            //var lurePokemon =
            //    response.MapCells.SelectMany(f => f.Forts).Where(t => t.LureInfo != null).Select(l => new MapPokemon()
            //    {
            //        EncounterId = l.LureInfo.EncounterId,
            //        ExpirationTimestampMs = l.LureInfo.LureExpiresTimestampMs,
            //        Latitude = l.Latitude,
            //        Longitude = l.Longitude,
            //        PokemonId = l.LureInfo.ActivePokemonId,
            //        SpawnPointId = l.Id
            //    });
            //_pokemons.AddRange(lurePokemon);
            _pokemons = _pokemons.Where(t => t.ExpirationTimestampMs > DateTime.UtcNow.ToUnixTime()).ToList();
            return response;
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