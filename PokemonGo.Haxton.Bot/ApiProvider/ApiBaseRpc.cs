using Google.Protobuf;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Helpers;
using PokemonGo.RocketAPI.HttpClient;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public interface IApiBaseRpc
    {
        Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestType type, IMessage message) where TRequest : IMessage<TRequest> where TResponsePayload : IMessage<TResponsePayload>, new();

        Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new();

        Task<ResponseEnvelope> PostProto<TRequest>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>;

        Task<ResponseEnvelope> PostProto<TRequest>(string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>;

        RequestBuilder RequestBuilder { get; }
    }

    public class ApiBaseRpc : IApiBaseRpc
    {
        private readonly IApiClient _client;
        private readonly PokemonHttpClient _pokemonHttpClient;
        private int _requestPerSecond { get; set; }
        private readonly object _lockObject = new object();

        public RequestBuilder RequestBuilder
            => new RequestBuilder(_client.AuthToken, _client.AuthType, _client.CurrentLatitude,
                _client.CurrentLongitude, _client.CurrentAltitude, _client.AuthTicket);

        private string ApiUrl => $"https://{_client.ApiUrl}/rpc";

        public ApiBaseRpc(IApiClient client)
        {
            _client = client;
            _pokemonHttpClient = new PokemonHttpClient();

            Task.Run(UpdateCounter);
        }

        private async Task UpdateCounter()
        {
            while (true)
            {
                _requestPerSecond = 0;
                await Task.Delay(1000);
            }
        }

        public async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestType type, IMessage message) where TRequest : IMessage<TRequest> where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            var requestEnvelops = RequestBuilder.GetRequestEnvelope(type, message);
            //lock (_lockObject)
            //{
            //    while (_requestPerSecond >= 3)
            //        Thread.Sleep(100);
            //    _requestPerSecond++;
            //}
            return await _pokemonHttpClient.PostProtoPayload<TRequest, TResponsePayload>(ApiUrl, requestEnvelops);
        }

        public async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            //lock (_lockObject)
            //{
            //    while (_requestPerSecond >= 3)
            //        Thread.Sleep(100);
            //    _requestPerSecond++;
            //}

            return await _pokemonHttpClient.PostProtoPayload<TRequest, TResponsePayload>(ApiUrl, requestEnvelope);
        }

        public async Task<ResponseEnvelope> PostProto<TRequest>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            //lock (_lockObject)
            //{
            //    while (_requestPerSecond >= 3)
            //        Thread.Sleep(100);
            //    _requestPerSecond++;
            //}
            return await _pokemonHttpClient.PostProto<TRequest>(ApiUrl, requestEnvelope);
        }

        public async Task<ResponseEnvelope> PostProto<TRequest>(string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            //lock (_lockObject)
            //{
            //    while (_requestPerSecond >= 3)
            //        Thread.Sleep(100);
            //    _requestPerSecond++;
            //}
            return await _pokemonHttpClient.PostProto<TRequest>(url, requestEnvelope);
        }
    }
}