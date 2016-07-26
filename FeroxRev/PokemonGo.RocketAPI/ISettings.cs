using PokemonGo.RocketAPI.Enums;

namespace PokemonGo.RocketAPI
{
    public interface ISettings
    {
        AuthType AuthType { get; }
        string DefaultLatitude { get; }
        string DefaultLongitude { get; }
        string DefaultAltitude { get; }
        string GoogleRefreshToken { get; set; }
        string PtcPassword { get; }
        string PtcUsername { get; }
        string ApiUrl { get; set; }
    }
}