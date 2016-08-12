using Google.Protobuf;
using POGOProtos.Networking;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace PokemonGo.RocketAPI.Helpers
{
    public class RequestBuilder
    {
        private readonly string _authToken;

        private readonly AuthType _authType;

        private readonly double _latitude;

        private readonly double _longitude;

        private readonly double _altitude;

        private readonly AuthTicket _authTicket;

        private static readonly Stopwatch InternalWatch = new Stopwatch();

        private readonly ISettings _settings;

        private static readonly Random RandomDevice = new Random();

        public RequestBuilder(string authToken, AuthType authType, double latitude, double longitude, double altitude, ISettings settings, AuthTicket authTicket = null)
        {
            _authToken = authToken;
            _authType = authType;
            _latitude = latitude;
            _longitude = longitude;
            _altitude = altitude;
            this._settings = settings;
            _authTicket = authTicket;
            if (!InternalWatch.IsRunning)
            {
                InternalWatch.Start();
            }
        }

        private Unknown6 GenerateSignature(IEnumerable<IMessage> requests)
        {
            var sig = new Signature();
            sig.TimestampSinceStart = (ulong)InternalWatch.ElapsedMilliseconds;
            sig.Timestamp = (ulong)DateTime.UtcNow.ToUnixTime();
            sig.SensorInfo = new Signature.Types.SensorInfo
            {
                AccelNormalizedZ = GenRandom(9.8),
                AccelNormalizedX = GenRandom(0.02),
                AccelNormalizedY = GenRandom(0.3),
                TimestampSnapshot = (ulong)(InternalWatch.ElapsedMilliseconds - 230L),
                MagnetometerX = GenRandom(12271042913198472.0),
                MagnetometerY = GenRandom(-0.015570580959320068),
                MagnetometerZ = GenRandom(0.010850906372070313),
                AngleNormalizedX = GenRandom(17.950439453125),
                AngleNormalizedY = GenRandom(-23.36273193359375),
                AngleNormalizedZ = GenRandom(-48.8250732421875),
                AccelRawX = GenRandom(-0.0120010357350111),
                AccelRawY = GenRandom(-0.04214850440621376),
                AccelRawZ = GenRandom(0.94571763277053833),
                GyroscopeRawX = GenRandom(7.62939453125E-05),
                GyroscopeRawY = GenRandom(-0.00054931640625),
                GyroscopeRawZ = GenRandom(0.0024566650390625),
                AccelerometerAxes = 3uL
            };
            sig.DeviceInfo = new Signature.Types.DeviceInfo
            {
                DeviceId = (_settings.DeviceId ?? $"1{RandomDevice.Next(12345)}"),
                AndroidBoardName = (_settings.AndroidBoardName ?? $"plsnobanerino{RandomDevice.Next(12345)}"),
                AndroidBootloader = (_settings.AndroidBootloader ?? $"andriod{RandomDevice.Next(12345)}"),
                DeviceBrand = (_settings.DeviceBrand ?? $"samsung{RandomDevice.Next(12345)}"),
                DeviceModel = (_settings.DeviceModel ?? $"1.0{RandomDevice.Next(12345)}"),
                DeviceModelIdentifier = (_settings.DeviceModelIdentifier ?? $"1.0{RandomDevice.Next(12345)}"),
                DeviceModelBoot = (_settings.DeviceModelBoot ?? $"1234{RandomDevice.Next(12345)}"),
                HardwareManufacturer = (_settings.HardwareManufacturer ?? $"sprint{RandomDevice.Next(12345)}"),
                HardwareModel = (_settings.HardwareModel ?? $"thisone{RandomDevice.Next(12345)}"),
                FirmwareBrand = (_settings.FirmwareBrand ?? $"thatone{RandomDevice.Next(12345)}"),
                FirmwareTags = (_settings.FirmwareTags ?? $"123{RandomDevice.Next(12345)}"),
                FirmwareType = (_settings.FirmwareType ?? $"n{RandomDevice.Next(12345)}o"),
                FirmwareFingerprint = (_settings.FirmwareFingerprint ?? $"kth{RandomDevice.Next(12345)}x")
            };
            sig.LocationFix.Add(new Signature.Types.LocationFix
            {
                Provider = "network",
                Latitude = (float)_latitude,
                Longitude = (float)_longitude,
                Altitude = (float)_altitude,
                TimestampSinceStart = (ulong)(InternalWatch.ElapsedMilliseconds - 200L),
                Floor = 3u,
                LocationType = 1uL
            });
            var x = new xxHash(32, 461656632uL);
            var firstHash = BitConverter.ToUInt32(x.ComputeHash(_authTicket.ToByteArray()), 0);
            x = new xxHash(32, (ulong)firstHash);
            var locationBytes = BitConverter.GetBytes(_latitude).Reverse<byte>().Concat(BitConverter.GetBytes(_longitude).Reverse<byte>()).Concat(BitConverter.GetBytes(_altitude).Reverse<byte>()).ToArray<byte>();
            sig.LocationHash1 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            x = new xxHash(32, 461656632uL);
            sig.LocationHash2 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            x = new xxHash(64, 461656632uL);
            var seed = BitConverter.ToUInt64(x.ComputeHash(_authTicket.ToByteArray()), 0);
            x = new xxHash(64, seed);
            foreach (var req in requests)
            {
                sig.RequestHash.Add(BitConverter.ToUInt64(x.ComputeHash(req.ToByteArray()), 0));
            }
            sig.Unk22 = ByteString.CopyFrom(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            var val = new Unknown6
            {
                RequestType = 6,
                Unknown2 = new Unknown6.Types.Unknown2 { Unknown1 = ByteString.CopyFrom(Encrypt(sig.ToByteArray())) }
            };
            return val;
        }

        private byte[] Encrypt(byte[] bytes)
        {
            var outputLength = 32 + bytes.Length + (256 - bytes.Length % 256);
            var ptr = Marshal.AllocHGlobal(outputLength);
            var ptrOutput = Marshal.AllocHGlobal(outputLength);
            FillMemory(ptr, (uint)outputLength, 0);
            FillMemory(ptrOutput, (uint)outputLength, 0);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            try
            {
                var outputSize = outputLength;
                EncryptNative(ptr, bytes.Length, new byte[32], 32, ptrOutput, out outputSize);
            }
            catch (Exception arg_5A_0)
            {
                Console.WriteLine(arg_5A_0.Message);
            }
            var output = new byte[outputLength];
            Marshal.Copy(ptrOutput, output, 0, outputLength);
            return output;
        }

        [DllImport("encrypt.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "encrypt")]
        private static extern void EncryptNative(IntPtr arr, int length, byte[] iv, int ivsize, IntPtr output, out int outputSize);

        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory")]
        private static extern void FillMemory(IntPtr destination, uint length, byte fill);

        public RequestEnvelope GetRequestEnvelope(params Request[] customRequests)
        {
            return new RequestEnvelope
            {
                StatusCode = 2,
                RequestId = 1469378659230941192uL,
                Requests =
                {
                    customRequests
                },
                Latitude = _latitude,
                Longitude = _longitude,
                Altitude = _altitude,
                AuthTicket = _authTicket,
                Unknown12 = 989L,
                Unknown6 =
                {
                    GenerateSignature(customRequests)
                }
            };
        }

        public RequestEnvelope GetInitialRequestEnvelope(params Request[] customRequests)
        {
            return new RequestEnvelope
            {
                StatusCode = 2,
                RequestId = 1469378659230941192uL,
                Requests =
                {
                    customRequests
                },
                Latitude = _latitude,
                Longitude = _longitude,
                Altitude = _altitude,
                AuthInfo = new RequestEnvelope.Types.AuthInfo
                {
                    Provider = ((_authType == AuthType.Google) ? "google" : "ptc"),
                    Token = new RequestEnvelope.Types.AuthInfo.Types.JWT
                    {
                        Contents = _authToken,
                        Unknown2 = 14
                    }
                },
                Unknown12 = 989L
            };
        }

        public RequestEnvelope GetRequestEnvelope(RequestType type, IMessage message)
        {
            return GetRequestEnvelope(new Request[]
            {
                new Request
                {
                    RequestType = type,
                    RequestMessage = message.ToByteString()
                }
            });
        }

        public static double GenRandom(double num)
        {
            var randomFactor = 0.3f;
            var randomMin = num * (double)(1f - randomFactor);
            var randomMax = num * (double)(1f + randomFactor);
            return RandomDevice.NextDouble() * (randomMax - randomMin) + randomMin;
        }
    }
}