using POGOProtos.Networking.Responses;
using PokemonGo.Haxton.Bot.ApiProvider;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Navigation
{
    public interface IPoGoFort
    {
        Task<FortDetailsResponse> GetFort(string fortId, double fortLatitude, double fortLongitude);

        Task<FortSearchResponse> SearchFort(string id, double latitude, double longitude);
    }

    public class PoGoFort : IPoGoFort
    {
        private readonly IApiFort _fort;

        public PoGoFort(IApiFort fort)
        {
            _fort = fort;
        }

        public async Task<FortDetailsResponse> GetFort(string fortId, double fortLatitude, double fortLongitude)
        {
            return await _fort.GetFort(fortId, fortLatitude, fortLongitude);
        }

        public async Task<FortSearchResponse> SearchFort(string id, double latitude, double longitude)
        {
            return await _fort.SearchFort(id, latitude, longitude);
        }
    }
}