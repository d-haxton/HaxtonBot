using PokemonGo.RocketAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.HttpClient
{
    public class PokemonHttpClient : System.Net.Http.HttpClient
    {
        private static HttpClientHandler Handler(string proxy)
        {
            var httpHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false,
                UseProxy = !string.IsNullOrWhiteSpace(proxy),
                UseCookies = true
            };
            if (httpHandler.UseProxy)
                httpHandler.Proxy = new WebProxy(proxy, false);
            return httpHandler;
        }

        public PokemonHttpClient(string proxy) : base(new RetryHandler(Handler(proxy)))
        {
            DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Niantic App");
            DefaultRequestHeaders.ExpectContinue = false;
            DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
            DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
        }
    }
}