using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Helpers
{
    internal class RetryHandler : DelegatingHandler
    {
        private const int MaxRetries = 25;

        public RetryHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i <= MaxRetries; i++)
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    if (response.StatusCode == HttpStatusCode.BadGateway)
                    {
                        Debug.WriteLine($"[#{i} of {MaxRetries}] retry request {request.RequestUri}");
                        if (i < MaxRetries)
                        {
                            await Task.Delay(1000, cancellationToken);
                            continue;
                        }
                    }

                    return response;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }
    }
}