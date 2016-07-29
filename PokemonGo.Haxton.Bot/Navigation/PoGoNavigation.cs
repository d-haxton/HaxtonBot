using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using PokemonGo.Haxton.Bot.ApiProvider;
using PokemonGo.Haxton.Bot.Utilities;
using System;
using System.Device.Location;
using System.Globalization;
using System.Threading.Tasks;

namespace PokemonGo.Haxton.Bot.Navigation
{
    public interface IPoGoNavigation
    {
        double CurrentLatitude { get; set; }
        double CurrentLongitude { get; set; }

        Task<PlayerUpdateResponse> HumanLikeWalking(GeoCoordinate targetLocation, double walkingSpeedInKilometersPerHour, Action functionExecutedWhileWalking);

        Task<PlayerUpdateResponse> HumanPathWalking(GpxReader.Trkpt trk,
            double walkingSpeedInKilometersPerHour, Action functionExecutedWhileWalking);

        Task TeleportToPokestop(FortData closestPokestop);

        Task TeleportToLocation(double lat, double longitude);
    }

    public class PoGoNavigation : IPoGoNavigation
    {
        private const double SpeedDownTo = 10 / 3.6;
        private readonly IApiPlayer _player;
        private readonly IApiClient _client;
        private readonly ILogicSettings _logicSettings;

        public PoGoNavigation(IApiPlayer player, IApiClient client, ILogicSettings logicSettings)
        {
            _player = player;
            _client = client;
            _logicSettings = logicSettings;
        }

        public double CurrentLatitude
        {
            get { return _client.CurrentLatitude; }
            set { _client.CurrentLatitude = value; }
        }

        public double CurrentLongitude
        {
            get { return _client.CurrentLongitude; }
            set { _client.CurrentLongitude = value; }
        }

        public async Task<PlayerUpdateResponse> HumanLikeWalking(GeoCoordinate targetLocation, double walkingSpeedInKilometersPerHour, Action functionExecutedWhileWalking)
        {
            // todo:: gut initial logic and put everything in a loop correctly
            var speedInMetersPerSecond = walkingSpeedInKilometersPerHour / 3.6;

            var sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            var distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);
            // Logger.Write($"Distance to target location: {distanceToTarget:0.##} meters. Will take {distanceToTarget/speedInMetersPerSecond:0.##} seconds!", LogLevel.Info);

            var nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
            var nextWaypointDistance = speedInMetersPerSecond;
            var waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

            //Initial walking
            var requestSendDateTime = DateTime.Now;
            PlayerUpdateResponse result;
            await _player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude, _client.Settings.DefaultAltitude);

            do
            {
                var millisecondsUntilGetUpdatePlayerLocationResponse =
                    (DateTime.Now - requestSendDateTime).TotalMilliseconds;

                sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                var currentDistanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);

                if (currentDistanceToTarget < 15)
                {
                    if (speedInMetersPerSecond > SpeedDownTo)
                    {
                        //Logger.Write("We are within 40 meters of the target. Speeding down to 10 km/h to not pass the target.", LogLevel.Info);
                        speedInMetersPerSecond = SpeedDownTo;
                    }
                }

                nextWaypointDistance = Math.Min(currentDistanceToTarget,
                    millisecondsUntilGetUpdatePlayerLocationResponse / 1000 * speedInMetersPerSecond);
                nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

                requestSendDateTime = DateTime.Now;
                result =
                    await
                        _player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude,
                            _client.Settings.DefaultAltitude);

                //if (result.WildPokemons.Count > 0)
                functionExecutedWhileWalking?.Invoke(); // look for pokemon

                if (_logicSettings.Teleport == false)
                    await Task.Delay(Math.Min((int)(currentDistanceToTarget / speedInMetersPerSecond * 1000), 1500));
            } while (LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation) >= 10);

            return result;
        }

        public async Task<PlayerUpdateResponse> HumanPathWalking(GpxReader.Trkpt trk,
            double walkingSpeedInKilometersPerHour, Action functionExecutedWhileWalking)
        {
            //PlayerUpdateResponse result = null;

            var targetLocation = new GeoCoordinate(Convert.ToDouble(trk.Lat, CultureInfo.InvariantCulture), Convert.ToDouble(trk.Lon, CultureInfo.InvariantCulture));

            var speedInMetersPerSecond = walkingSpeedInKilometersPerHour / 3.6;

            var sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            var distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);
            // Logger.Write($"Distance to target location: {distanceToTarget:0.##} meters. Will take {distanceToTarget/speedInMetersPerSecond:0.##} seconds!", LogLevel.Info);

            var nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
            var nextWaypointDistance = speedInMetersPerSecond;
            var waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing,
                Convert.ToDouble(trk.Ele, CultureInfo.InvariantCulture));

            //Initial walking

            var requestSendDateTime = DateTime.Now;
            var result =
                await
                    _player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude, waypoint.Altitude);

            do
            {
                var millisecondsUntilGetUpdatePlayerLocationResponse =
                    (DateTime.Now - requestSendDateTime).TotalMilliseconds;

                sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                var currentDistanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);

                //if (currentDistanceToTarget < 40)
                //{
                //    if (speedInMetersPerSecond > SpeedDownTo)
                //    {
                //        //Logger.Write("We are within 40 meters of the target. Speeding down to 10 km/h to not pass the target.", LogLevel.Info);
                //        speedInMetersPerSecond = SpeedDownTo;
                //    }
                //}

                nextWaypointDistance = Math.Min(currentDistanceToTarget,
                    millisecondsUntilGetUpdatePlayerLocationResponse / 1000 * speedInMetersPerSecond);
                nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

                requestSendDateTime = DateTime.Now;
                result =
                    await
                        _player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude,
                            waypoint.Altitude);

                functionExecutedWhileWalking?.Invoke(); // look for pokemon & hit stops

                await Task.Delay(Math.Min((int)(distanceToTarget / speedInMetersPerSecond * 1000), 3000));
            } while (LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation) >= 30);

            return result;
        }

        public async Task TeleportToPokestop(FortData closestPokestop)
        {
            if (closestPokestop?.Latitude == null)
                return;
            //if (closestPokestop.Latitude > 35)
            //    return;
            await _player.UpdatePlayerLocation(closestPokestop.Latitude, closestPokestop.Longitude, 10);
        }

        public async Task TeleportToLocation(double lat, double longitude)
        {
            //if (lat > 35)
            //    return;
            await _player.UpdatePlayerLocation(lat, longitude, 10);
        }
    }
}