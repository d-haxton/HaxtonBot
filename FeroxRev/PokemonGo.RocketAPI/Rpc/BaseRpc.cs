using Google.Protobuf;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Rpc
{
    public class BaseRpc
    {
        protected Client _client;
        protected RequestBuilder RequestBuilder => new RequestBuilder(_client.AuthToken, _client.AuthType, _client.CurrentLatitude, _client.CurrentLongitude, _client.CurrentAltitude, _client.AuthTicket);
        protected string ApiUrl => $"https://{_client.ApiUrl}/rpc";

        protected BaseRpc(Client client)
        {
            _client = client;
        }

        protected async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestType type, IMessage message) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            var requestEnvelops = RequestBuilder.GetRequestEnvelope(type, message);
            return await _client.PokemonHttpClient.PostProtoPayload<TRequest, TResponsePayload>(ApiUrl, requestEnvelops, null);
        }

        protected async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            return await _client.PokemonHttpClient.PostProtoPayload<TRequest, TResponsePayload>(ApiUrl, requestEnvelope, null);
        }

        protected async Task<ResponseEnvelope> PostProto<TRequest>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            return await _client.PokemonHttpClient.PostProto<TRequest>(ApiUrl, requestEnvelope);
        }

        protected async Task<ResponseEnvelope> PostProto<TRequest>(string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            return await _client.PokemonHttpClient.PostProto<TRequest>(url, requestEnvelope);
        }
    }
}