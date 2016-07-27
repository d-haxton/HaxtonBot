using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public interface IApiMisc
    {
        Task<ClaimCodenameResponse> ClaimCodename(string codename);

        Task<CheckCodenameAvailableResponse> CheckCodenameAvailable(string codename);

        Task<GetSuggestedCodenamesResponse> GetSuggestedCodenames();

        Task<EchoResponse> SendEcho();

        Task<EncounterTutorialCompleteResponse> MarkTutorialComplete();
    }

    public class ApiMisc : IApiMisc
    {
        private readonly IApiBaseRpc _baseRpc;

        public ApiMisc(IApiBaseRpc baseRpc)
        {
            _baseRpc = baseRpc;
        }

        public async Task<ClaimCodenameResponse> ClaimCodename(string codename)
        {
            return
                await
                    _baseRpc.PostProtoPayload<Request, ClaimCodenameResponse>(RequestType.ClaimCodename,
                        new ClaimCodenameMessage()
                        {
                            Codename = codename
                        });
        }

        public async Task<CheckCodenameAvailableResponse> CheckCodenameAvailable(string codename)
        {
            return
                await
                    _baseRpc.PostProtoPayload<Request, CheckCodenameAvailableResponse>(RequestType.CheckCodenameAvailable,
                        new CheckCodenameAvailableMessage()
                        {
                            Codename = codename
                        });
        }

        public async Task<GetSuggestedCodenamesResponse> GetSuggestedCodenames()
        {
            return await _baseRpc.PostProtoPayload<Request, GetSuggestedCodenamesResponse>(RequestType.GetSuggestedCodenames, new GetSuggestedCodenamesMessage());
        }

        public async Task<EchoResponse> SendEcho()
        {
            return await _baseRpc.PostProtoPayload<Request, EchoResponse>(RequestType.Echo, new EchoMessage());
        }

        public async Task<EncounterTutorialCompleteResponse> MarkTutorialComplete()
        {
            return await _baseRpc.PostProtoPayload<Request, EncounterTutorialCompleteResponse>(RequestType.MarkTutorialComplete, new MarkTutorialCompleteMessage());
        }
    }
}