using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.ApiProvider
{
    public interface IApiInventory
    {
        Task<ReleasePokemonResponse> TransferPokemon(ulong pokemonId);

        Task<EvolvePokemonResponse> EvolvePokemon(ulong pokemonId);

        Task<UpgradePokemonResponse> UpgradePokemon(ulong pokemonId);

        Task<GetInventoryResponse> GetInventory();

        Task<RecycleInventoryItemResponse> RecycleItem(ItemId itemId, int amount);

        Task<UseItemXpBoostResponse> UseItemXpBoost();

        Task<UseItemEggIncubatorResponse> UseItemEggIncubator(string itemId, ulong pokemonId);

        Task<GetHatchedEggsResponse> GetHatchedEgg();

        Task<UseItemPotionResponse> UseItemPotion(ItemId itemId, ulong pokemonId);

        Task<UseItemEggIncubatorResponse> UseItemRevive(ItemId itemId, ulong pokemonId);

        Task<UseIncenseResponse> UseIncense(ItemId incenseType);

        Task<UseItemGymResponse> UseItemInGym(string gymId, ItemId itemId);

        Task<NicknamePokemonResponse> NicknamePokemon(ulong pokemonId, string nickName);

        Task<SetFavoritePokemonResponse> SetFavoritePokemon(ulong pokemonId, bool isFavorite);
    }

    public class ApiInventory : IApiInventory
    {
        private readonly IApiBaseRpc _baseRpc;
        private readonly IApiClient _client;

        public ApiInventory(IApiBaseRpc baseRpc, IApiClient client)
        {
            _baseRpc = baseRpc;
            _client = client;
        }

        public async Task<ReleasePokemonResponse> TransferPokemon(ulong pokemonId)
        {
            var message = new ReleasePokemonMessage
            {
                PokemonId = pokemonId
            };

            return await _baseRpc.PostProtoPayload<Request, ReleasePokemonResponse>(RequestType.ReleasePokemon, message);
        }

        public async Task<EvolvePokemonResponse> EvolvePokemon(ulong pokemonId)
        {
            var message = new EvolvePokemonMessage
            {
                PokemonId = pokemonId
            };

            return await _baseRpc.PostProtoPayload<Request, EvolvePokemonResponse>(RequestType.EvolvePokemon, message);
        }

        public async Task<UpgradePokemonResponse> UpgradePokemon(ulong pokemonId)
        {
            var message = new UpgradePokemonMessage()
            {
                PokemonId = pokemonId
            };

            return await _baseRpc.PostProtoPayload<Request, UpgradePokemonResponse>(RequestType.UpgradePokemon, message);
        }

        public async Task<GetInventoryResponse> GetInventory()
        {
            return await _baseRpc.PostProtoPayload<Request, GetInventoryResponse>(RequestType.GetInventory, new GetInventoryMessage());
        }

        public async Task<RecycleInventoryItemResponse> RecycleItem(ItemId itemId, int amount)
        {
            var message = new RecycleInventoryItemMessage
            {
                ItemId = itemId,
                Count = amount
            };

            return await _baseRpc.PostProtoPayload<Request, RecycleInventoryItemResponse>(RequestType.RecycleInventoryItem, message);
        }

        public async Task<UseItemXpBoostResponse> UseItemXpBoost()
        {
            var message = new UseItemXpBoostMessage()
            {
                ItemId = ItemId.ItemLuckyEgg
            };

            return await _baseRpc.PostProtoPayload<Request, UseItemXpBoostResponse>(RequestType.UseItemXpBoost, message);
        }

        public async Task<UseItemEggIncubatorResponse> UseItemEggIncubator(string itemId, ulong pokemonId)
        {
            var message = new UseItemEggIncubatorMessage()
            {
                ItemId = itemId,
                PokemonId = pokemonId
            };

            return await _baseRpc.PostProtoPayload<Request, UseItemEggIncubatorResponse>(RequestType.UseItemEggIncubator, message);
        }

        public async Task<GetHatchedEggsResponse> GetHatchedEgg()
        {
            return await _baseRpc.PostProtoPayload<Request, GetHatchedEggsResponse>(RequestType.GetHatchedEggs, new GetHatchedEggsMessage());
        }

        public async Task<UseItemPotionResponse> UseItemPotion(ItemId itemId, ulong pokemonId)
        {
            var message = new UseItemPotionMessage()
            {
                ItemId = itemId,
                PokemonId = pokemonId
            };

            return await _baseRpc.PostProtoPayload<Request, UseItemPotionResponse>(RequestType.UseItemPotion, message);
        }

        public async Task<UseItemEggIncubatorResponse> UseItemRevive(ItemId itemId, ulong pokemonId)
        {
            var message = new UseItemReviveMessage()
            {
                ItemId = itemId,
                PokemonId = pokemonId
            };

            return await _baseRpc.PostProtoPayload<Request, UseItemEggIncubatorResponse>(RequestType.UseItemEggIncubator, message);
        }

        public async Task<UseIncenseResponse> UseIncense(ItemId incenseType)
        {
            var message = new UseIncenseMessage()
            {
                IncenseType = incenseType
            };

            return await _baseRpc.PostProtoPayload<Request, UseIncenseResponse>(RequestType.UseIncense, message);
        }

        public async Task<UseItemGymResponse> UseItemInGym(string gymId, ItemId itemId)
        {
            var message = new UseItemGymMessage()
            {
                ItemId = itemId,
                GymId = gymId,
                PlayerLatitude = _client.CurrentLatitude,
                PlayerLongitude = _client.CurrentLongitude
            };

            return await _baseRpc.PostProtoPayload<Request, UseItemGymResponse>(RequestType.UseItemGym, message);
        }

        public async Task<NicknamePokemonResponse> NicknamePokemon(ulong pokemonId, string nickName)
        {
            var message = new NicknamePokemonMessage()
            {
                PokemonId = pokemonId,
                Nickname = nickName
            };

            return await _baseRpc.PostProtoPayload<Request, NicknamePokemonResponse>(RequestType.NicknamePokemon, message);
        }

        public async Task<SetFavoritePokemonResponse> SetFavoritePokemon(ulong pokemonId, bool isFavorite)
        {
            var message = new SetFavoritePokemonMessage()
            {
                PokemonId = pokemonId,
                IsFavorite = isFavorite
            };

            return await _baseRpc.PostProtoPayload<Request, SetFavoritePokemonResponse>(RequestType.SetFavoritePokemon, message);
        }
    }
}