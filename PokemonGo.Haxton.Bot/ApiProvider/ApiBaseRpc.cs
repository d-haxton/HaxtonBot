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
        RequestBuilder RequestBuilder { get; }

        Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestType type, IMessage message) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new();

        Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new();

        Task<Tuple<T1, T2>> PostProtoPayload<TRequest, T1, T2>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new();

        Task<Tuple<T1, T2, T3>> PostProtoPayload<TRequest, T1, T2, T3>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new();

        Task<Tuple<T1, T2, T3, T4>> PostProtoPayload<TRequest, T1, T2, T3, T4>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
            where T4 : class, IMessage<T4>, new();

        Task<Tuple<T1, T2, T3, T4, T5>> PostProtoPayload<TRequest, T1, T2, T3, T4, T5>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
            where T4 : class, IMessage<T4>, new()
            where T5 : class, IMessage<T5>, new();

        Task<IMessage[]> PostProtoPayload<TRequest>(RequestEnvelope requestEnvelope, params Type[] responseTypes) where TRequest : IMessage<TRequest>;

        Task<ResponseEnvelope> PostProto<TRequest>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>;

        Task<ResponseEnvelope> PostProto<TRequest>(string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>;
    }

    public class ApiBaseRpc : IApiBaseRpc
    {
        private readonly IApiClient _client;
        private readonly ISettings _settings;
        private readonly PokemonHttpClient _pokemonHttpClient;

        public RequestBuilder RequestBuilder
            => new RequestBuilder(_client.AuthToken, _client.AuthType, _client.CurrentLatitude,
                _client.CurrentLongitude, _client.CurrentAltitude, _settings, _client.AuthTicket);

        private string ApiUrl => $"https://{_client.ApiUrl}/rpc";

        public ApiBaseRpc(IApiClient client, ISettings settings)
        {
            _client = client;
            _settings = settings;
            _pokemonHttpClient = new PokemonHttpClient(settings.Proxy);
        }

        public async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestType type, IMessage message) where TRequest : IMessage<TRequest>
    where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            var requestEnvelops = RequestBuilder.GetRequestEnvelope(type, message);
            return await _pokemonHttpClient.PostProtoPayload<TRequest, TResponsePayload>(ApiUrl, requestEnvelops, _client.ApiFailure);
        }

        public async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            return await _pokemonHttpClient.PostProtoPayload<TRequest, TResponsePayload>(ApiUrl, requestEnvelope, _client.ApiFailure);
        }

        public async Task<Tuple<T1, T2>> PostProtoPayload<TRequest, T1, T2>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
        {
            var responses = await PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2));
            return new Tuple<T1, T2>(responses[0] as T1, responses[1] as T2);
        }

        public async Task<Tuple<T1, T2, T3>> PostProtoPayload<TRequest, T1, T2, T3>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
        {
            var responses = await PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2), typeof(T3));
            return new Tuple<T1, T2, T3>(responses[0] as T1, responses[1] as T2, responses[2] as T3);
        }

        public async Task<Tuple<T1, T2, T3, T4>> PostProtoPayload<TRequest, T1, T2, T3, T4>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
            where T4 : class, IMessage<T4>, new()
        {
            var responses = await PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
            return new Tuple<T1, T2, T3, T4>(responses[0] as T1, responses[1] as T2, responses[2] as T3, responses[3] as T4);
        }

        public async Task<Tuple<T1, T2, T3, T4, T5>> PostProtoPayload<TRequest, T1, T2, T3, T4, T5>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
            where T4 : class, IMessage<T4>, new()
            where T5 : class, IMessage<T5>, new()
        {
            var responses = await PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
            return new Tuple<T1, T2, T3, T4, T5>(responses[0] as T1, responses[1] as T2, responses[2] as T3, responses[3] as T4, responses[3] as T5);
        }

        public async Task<IMessage[]> PostProtoPayload<TRequest>(RequestEnvelope requestEnvelope, params Type[] responseTypes) where TRequest : IMessage<TRequest>
        {
            return await _pokemonHttpClient.PostProtoPayload<TRequest>(ApiUrl, requestEnvelope, _client.ApiFailure, responseTypes);
        }

        public async Task<ResponseEnvelope> PostProto<TRequest>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            return await _pokemonHttpClient.PostProto<TRequest>(ApiUrl, requestEnvelope);
        }

        public async Task<ResponseEnvelope> PostProto<TRequest>(string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            return await _pokemonHttpClient.PostProto<TRequest>(url, requestEnvelope);
        }
    }
}