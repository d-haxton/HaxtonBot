using POGOProtos.Networking.Envelopes;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public interface IApiClient
    {
        ISettings Settings { get; set; }
        double CurrentLatitude { get; set; }
        double CurrentLongitude { get; set; }
        double CurrentAltitude { get; set; }
        AuthType AuthType { get; set; }
        string ApiUrl { get; set; }
        AuthTicket AuthTicket { get; set; }
        string AuthToken { get; set; }
    }

    public class ApiClient : IApiClient
    {
        public ISettings Settings { get; set; }
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
        public double CurrentAltitude { get; set; }
        public AuthType AuthType { get; set; }

        public string ApiUrl
        {
            get { return Settings.ApiUrl; }
            set { Settings.ApiUrl = value; }
        }

        public AuthTicket AuthTicket { get; set; }
        public string AuthToken { get; set; }

        public ApiClient(ISettings settings)
        {
            Settings = settings;
            AuthType = settings.AuthType;

            CurrentAltitude = settings.DefaultAltitude;
            CurrentLongitude = settings.DefaultLongitude;
            CurrentLatitude = settings.DefaultLatitude;
        }
    }
}