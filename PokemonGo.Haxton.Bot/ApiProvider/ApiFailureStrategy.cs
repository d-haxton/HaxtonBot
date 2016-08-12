using POGOProtos.Networking.Envelopes;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public class ApiFailureStrategy : IApiFailureStrategy
    {
        public async Task<ApiOperation> HandleApiFailure(RequestEnvelope request, ResponseEnvelope response)
        {
            return ApiOperation.Abort;
        }

        public void HandleApiSuccess(RequestEnvelope request, ResponseEnvelope response)
        {
            //
        }
    }
}