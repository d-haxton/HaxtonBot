using POGOProtos.Map.Pokemon;
using System.Globalization;

namespace PokemonGo.Haxton.PoGoBot.Model
{
    public class PokemonModel
    {
        public PokemonModel(MapPokemon mapPokemon)
        {
            Name = mapPokemon.PokemonId.ToString();
            Id = mapPokemon.PokemonId + "";
            Latitude = mapPokemon.Latitude.ToString(CultureInfo.InvariantCulture);
            Longitude = mapPokemon.Longitude.ToString(CultureInfo.InvariantCulture);
            EncounterId = mapPokemon.EncounterId.ToString();
            SpawnId = mapPokemon.SpawnPointId;
            Expires = mapPokemon.ExpirationTimestampMs.ToString();
        }

        public PokemonModel(string name, string id, string latitude, string longitude, string encounterId, string spawnId, string expires)
        {
            Name = name;
            Id = id;
            Latitude = latitude;
            Longitude = longitude;
            EncounterId = encounterId;
            SpawnId = spawnId;
            Expires = expires;
        }

        public string Name { get; set; }
        public string Id { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string EncounterId { get; set; }
        public string SpawnId { get; set; }
        public string Expires { get; set; }
    }
}