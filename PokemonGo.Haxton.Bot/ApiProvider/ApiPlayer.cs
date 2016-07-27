using Google.Protobuf;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public interface IApiPlayer
    {
        Task<PlayerUpdateResponse> UpdatePlayerLocation(double latitude, double longitude, double altitude);

        void SetCoordinates(double lat, double lng, double altitude);

        Task<GetPlayerResponse> GetPlayer();

        Task<GetPlayerProfileResponse> GetPlayerProfile(string playerName);

        Task<CheckAwardedBadgesResponse> GetNewlyAwardedBadges();

        Task<CollectDailyBonusResponse> CollectDailyBonus();

        Task<CollectDailyDefenderBonusResponse> CollectDailyDefenderBonus();

        Task<EquipBadgeResponse> EquipBadge(BadgeType type);

        Task<LevelUpRewardsResponse> GetLevelUpRewards(int level);

        Task<SetAvatarResponse> SetAvatar(PlayerAvatar playerAvatar);

        Task<SetContactSettingsResponse> SetContactSetting(ContactSettings contactSettings);

        Task<SetPlayerTeamResponse> SetPlayerTeam(TeamColor teamColor);
    }

    public class ApiPlayer : IApiPlayer
    {
        private readonly IApiBaseRpc _baseRpc;
        private readonly IApiClient _client;

        public ApiPlayer(IApiBaseRpc baseRpc, IApiClient client)
        {
            _baseRpc = baseRpc;
            _client = client;
        }

        public async Task<PlayerUpdateResponse> UpdatePlayerLocation(double latitude, double longitude, double altitude)
        {
            SetCoordinates(latitude, longitude, altitude);
            var message = new PlayerUpdateMessage
            {
                Latitude = _client.CurrentLatitude,
                Longitude = _client.CurrentLongitude
            };

            var updatePlayerLocationRequestEnvelope = _baseRpc.RequestBuilder.GetRequestEnvelope(
                new Request
                {
                    RequestType = RequestType.PlayerUpdate,
                    RequestMessage = message.ToByteString()
                });

            return await _baseRpc.PostProtoPayload<Request, PlayerUpdateResponse>(updatePlayerLocationRequestEnvelope);
        }

        public void SetCoordinates(double lat, double lng, double altitude)
        {
            _client.CurrentLatitude = lat;
            _client.CurrentLongitude = lng;
            _client.CurrentAltitude = altitude;
        }

        public async Task<GetPlayerResponse> GetPlayer()
        {
            return await _baseRpc.PostProtoPayload<Request, GetPlayerResponse>(RequestType.GetPlayer, new GetPlayerMessage());
        }

        public async Task<GetPlayerProfileResponse> GetPlayerProfile(string playerName)
        {
            return await _baseRpc.PostProtoPayload<Request, GetPlayerProfileResponse>(RequestType.GetPlayerProfile, new GetPlayerProfileMessage()
            {
                PlayerName = playerName
            });
        }

        public async Task<CheckAwardedBadgesResponse> GetNewlyAwardedBadges()
        {
            return await _baseRpc.PostProtoPayload<Request, CheckAwardedBadgesResponse>(RequestType.CheckAwardedBadges, new CheckAwardedBadgesMessage());
        }

        public async Task<CollectDailyBonusResponse> CollectDailyBonus()
        {
            return await _baseRpc.PostProtoPayload<Request, CollectDailyBonusResponse>(RequestType.CollectDailyBonus, new CollectDailyBonusMessage());
        }

        public async Task<CollectDailyDefenderBonusResponse> CollectDailyDefenderBonus()
        {
            return await _baseRpc.PostProtoPayload<Request, CollectDailyDefenderBonusResponse>(RequestType.CollectDailyDefenderBonus, new CollectDailyDefenderBonusMessage());
        }

        public async Task<EquipBadgeResponse> EquipBadge(BadgeType type)
        {
            return await _baseRpc.PostProtoPayload<Request, EquipBadgeResponse>(RequestType.EquipBadge, new EquipBadgeMessage() { BadgeType = type });
        }

        public async Task<LevelUpRewardsResponse> GetLevelUpRewards(int level)
        {
            return await _baseRpc.PostProtoPayload<Request, LevelUpRewardsResponse>(RequestType.LevelUpRewards, new LevelUpRewardsMessage()
            {
                Level = level
            });
        }

        public async Task<SetAvatarResponse> SetAvatar(PlayerAvatar playerAvatar)
        {
            return await _baseRpc.PostProtoPayload<Request, SetAvatarResponse>(RequestType.SetAvatar, new SetAvatarMessage()
            {
                PlayerAvatar = playerAvatar
            });
        }

        public async Task<SetContactSettingsResponse> SetContactSetting(ContactSettings contactSettings)
        {
            return await _baseRpc.PostProtoPayload<Request, SetContactSettingsResponse>(RequestType.SetContactSettings, new SetContactSettingsMessage()
            {
                ContactSettings = contactSettings
            });
        }

        public async Task<SetPlayerTeamResponse> SetPlayerTeam(TeamColor teamColor)
        {
            return await _baseRpc.PostProtoPayload<Request, SetPlayerTeamResponse>(RequestType.SetPlayerTeam, new SetPlayerTeamMessage()
            {
                Team = teamColor
            });
        }
    }
}