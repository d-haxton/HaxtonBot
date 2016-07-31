using Google.Protobuf;
using POGOProtos.Networking.Envelopes;
using PokemonGo.RocketAPI.Exceptions;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(this System.Net.Http.HttpClient client,
            string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            var attempts = 0;
            ResponseEnvelope response;
            do
            {
                Debug.WriteLine($"Requesting {typeof(TResponsePayload).Name}");
                Thread.Sleep(150);
                response = await PostProto<TRequest>(client, url, requestEnvelope);

                //Decode payload
                //todo: multi-payload support
                attempts++;
            } while (response.Returns.Count == 0 && attempts < 25);
            if (attempts >= 25)
            {
                Console.WriteLine($"Failed to request packet {typeof(TResponsePayload).Name}");
                return new TResponsePayload();
            }

            var payload = response.Returns[0];
            var parsedPayload = new TResponsePayload();
            parsedPayload.MergeFrom(payload);
            return parsedPayload;
        }

        public static async Task<ResponseEnvelope> PostProto<TRequest>(this System.Net.Http.HttpClient client, string url,
            RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            //Encode payload and put in envelop, then send
            var data = requestEnvelope.ToByteString();
            var result = await client.PostAsync(url, new ByteArrayContent(data.ToByteArray()));

            //Decode message
            var responseData = await result.Content.ReadAsByteArrayAsync();
            var codedStream = new CodedInputStream(responseData);
            var decodedResponse = new ResponseEnvelope();
            decodedResponse.MergeFrom(codedStream);

            return decodedResponse;
        }
    }
}